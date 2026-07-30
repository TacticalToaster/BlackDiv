using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterController : MonoBehaviour
{
    [Header("Navigation")]
    public Vector3 TargetPosition;
    public bool Active = true;
    public HeliPath ActivePath;
    public int PathIndex = 0;
    public bool HoverNext = false;

    [Header("Movement")]
    public float MaxSpeed = 100f;
    public float MaxAcceleration = 18f;
    public float CruiseSpeed = 50f;

    [Header("Prediction")]
    [Tooltip("How far into the future the helicopter predicts its position.")]
    public float PredictionTime = 2.0f;

    [Header("Lift")]
    public float Gravity = 9.81f;
    public float HoverLift = 9.81f;
    public float MaxLift = 30f;
    public float LiftResponse = 15f;
    public float VerticalVelocityCompensation = 1.2f;
    [Tooltip("Vertical safety offset applied to path targets.")]
    public float PathAltitudeClearance = 2f;

    [Header("Attitude")]
    public float MaxPitch = 22f;
    public float MaxRoll = 35f;
    public float MaxYawRate = 90f;

    [Header("Angular Dynamics")]
    public float PitchAcceleration = 80f;
    public float RollAcceleration = 120f;
    public float YawAcceleration = 100f;

    [Header("Angular Response")]
    public float AttitudeResponseTime = 0.65f;

    [Header("Drag")]
    public float ForwardDrag = 0.15f;
    public float SideDrag = 2.5f;
    public float VerticalDrag = 1.0f;

    [Header("Approach")]
    public float ApproachBrakingAcceleration = 4.5f;
    public float ApproachBuffer = 8f;
    public float MinimumApproachSpeed = 3f;
    public float CruiseVelocityResponseTime = 1.2f;
    public float ApproachVelocityResponseTime = 0.65f;

    [Header("Hover Capture")]
    public float HoverEntrySpeed = 6f;
    public float HoverCaptureRadius = 30f;
    public float HoverExitRadius = 45f;
    public float HoverCaptureMargin = 8f;

    public float HoverPositionGain = 0.35f;
    public float HoverVelocityGain = 1.4f;
    public float MaxHoverAcceleration = 3.5f;

    public float HoverDeadZone = 0.5f;
    public float HoverVelocityDeadZone = 0.15f;
    
    [Header("Animation")]
    public Animator RotorAnimator;

    private bool _hoverMode;
    private Vector3 _hoverHeading;

    // =============================
    // Runtime State
    // =============================

    /// <summary>World velocity.</summary>
    private Vector3 _velocity;

    /// <summary>Pitch/Roll/Yaw velocity (deg/sec).</summary>
    private Vector3 _angularVelocity;

    /// <summary>Current lift force.</summary>
    private float _lift;

    /// <summary>Where we think we'll be after PredictionTime.</summary>
    private Vector3 _predictedPosition;

    /// <summary>Desired world acceleration.</summary>
    private Vector3 _desiredAcceleration;

    /// <summary>Desired look direction.</summary>
    private Vector3 _desiredForward;

    /// <summary>Desired lift direction.</summary>
    private Vector3 _desiredUp;

    // Cached transform
    private Transform _tr;

    void Awake()
    {
        _tr = transform;

        _lift = HoverLift;

        _desiredForward = _tr.forward;
        _desiredUp = _tr.up;
    }

    void Update()
    {
        if (!Active)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Flight(dt);
    }

    /// <summary>
    /// Main flight routine.
    /// Mirrors the structure of Source's Flight().
    /// </summary>
    private void Flight(float dt)
    {
        //PredictFuturePosition();
        
        CheckPath();

        ComputeDesiredAcceleration();

        UpdatePredictedPosition();

        ComputeDesiredOrientation();

        UpdateAngularMotion(dt);

        UpdateLift(dt);

        ApplyForces(dt);

        ApplyDrag(dt);

        Integrate(dt);
    }
    
    private void UpdatePredictedPosition()
    {
        float t = Mathf.Max(0.1f, PredictionTime);

        _predictedPosition =
            _tr.position +
            _velocity * t +
            0.5f * _desiredAcceleration * t * t;
    }

    private void CheckPath()
    {
        if (ActivePath == null) return;
        if (TargetPosition == Vector3.zero) TargetPosition = ActivePath.Nodes[0].transform.position;
        
        Vector3 positionError = TargetPosition - _tr.position;
        
        Vector3 horizontalError =
            Vector3.ProjectOnPlane(positionError, Vector3.up);

        float horizontalDistance = horizontalError.magnitude;

        if (horizontalDistance <= 20f && !HoverNext)
        {
            CheckNextPoint();
        }
    }

    private void CheckNextPoint()
    {
        PathIndex = ActivePath.Nodes.Count == PathIndex + 1 ? 0 : PathIndex + 1;
        if (PathIndex == ActivePath.Nodes.Count - 1)
        {
            HoverNext = true;
        }
        TargetPosition = ActivePath.Nodes[PathIndex].transform.position;
    }

    /// <summary>
    /// Estimate where the helicopter will be after PredictionTime.
    /// Source predicts ahead instead of reacting to the current position.
    /// </summary>
    private void PredictFuturePosition()
    {
        _predictedPosition = _tr.position + _velocity * PredictionTime;
    }

    /// <summary>
    /// Compute the world-space acceleration required to bring our
    /// predicted future position onto the destination.
    /// </summary>
    /*private void ComputeDesiredAcceleration()
    {
        Vector3 error = TargetPosition - _predictedPosition;

        // Solve for acceleration that removes the error over PredictionTime.
        _desiredAcceleration = (2f * error) /
                               (PredictionTime * PredictionTime);

        float vertical = Mathf.Clamp(
            _desiredAcceleration.y,
            -MaxAcceleration,
             MaxAcceleration);

        Vector3 horizontal = Vector3.ProjectOnPlane(
            _desiredAcceleration,
            Vector3.up);

        horizontal = Vector3.ClampMagnitude(
            horizontal,
            MaxAcceleration);

        _desiredAcceleration = horizontal;
        _desiredAcceleration.y = vertical;
    }*/
    private void ComputeDesiredAcceleration()
    {
        Vector3 positionError = TargetPosition - _tr.position;

        Vector3 horizontalError =
            Vector3.ProjectOnPlane(positionError, Vector3.up);

        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(_velocity, Vector3.up);

        float horizontalDistance = horizontalError.magnitude;
        float horizontalSpeed = horizontalVelocity.magnitude;

        /*
         * Begin capture early enough to stop.
         *
         * d = v� / 2a
         */
        float availableBrakingAcceleration =
        Mathf.Max(
            0.1f,
            Mathf.Max(
                MaxHoverAcceleration,
                ApproachBrakingAcceleration));

        float stoppingDistance =
            horizontalSpeed * horizontalSpeed /
            (2f * availableBrakingAcceleration);

        /*float captureDistance = Mathf.Max(
            HoverCaptureRadius,
            stoppingDistance + HoverCaptureMargin);*/
        
        float captureDistance =
            HoverCaptureRadius;

        /*
         * Hysteresis prevents repeatedly switching between cruise and hover.
         */
        if (!_hoverMode && horizontalDistance <= captureDistance && HoverNext)
        {
            _hoverMode = true;

            _hoverHeading =
                Vector3.ProjectOnPlane(_tr.forward, Vector3.up);

            if (_hoverHeading.sqrMagnitude < 0.001f)
                _hoverHeading = Vector3.forward;

            _hoverHeading.Normalize();
        }
        else if (_hoverMode && horizontalDistance > HoverExitRadius)
        {
            _hoverMode = false;
        }

        if (_hoverMode)
        {
            ComputeHoverAcceleration(positionError);
        }
        else if (ActivePath != null && !HoverNext)
        {
            ComputePathAcceleration();
        }
        else
        {
            ComputeCruiseAcceleration();
        }
    }
    
    private void ComputePathAcceleration()
    {
        float t = Mathf.Max(0.1f, PredictionTime);

        Vector3 positionError =
            TargetPosition - _tr.position;

        Vector3 horizontalError =
            Vector3.ProjectOnPlane(
                positionError,
                Vector3.up);

        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(
                _velocity,
                Vector3.up);

        /*
         * Horizontal guidance:
         * fly toward the point at MaxSpeed.
         */
        Vector3 desiredHorizontalVelocity = Vector3.zero;

        if (horizontalError.sqrMagnitude > 0.001f)
        {
            desiredHorizontalVelocity =
                horizontalError.normalized * MaxSpeed;
        }

        Vector3 horizontalAcceleration =
            (desiredHorizontalVelocity - horizontalVelocity) / t;

        horizontalAcceleration =
            Vector3.ClampMagnitude(
                horizontalAcceleration,
                MaxAcceleration);

        /*
         * Vertical guidance:
         *
         * Solve:
         * targetY = currentY + velocityY * t + 0.5 * accelerationY * t²
         *
         * This commands enough acceleration to reach the target altitude
         * after t seconds while accounting for current vertical velocity.
         */
        float verticalAcceleration =
            2f *
            (positionError.y - _velocity.y * t) /
            (t * t);

        verticalAcceleration = Mathf.Clamp(
            verticalAcceleration,
            -MaxAcceleration,
            MaxAcceleration);

        /*
         * Altitude has priority.
         *
         * When the target is level with or above us, do not permit a downward
         * acceleration request. Existing downward velocity is already included
         * in the equation above, so this should usually request upward force.
         */
        if (positionError.y >= 0f)
        {
            verticalAcceleration =
                Mathf.Max(0f, verticalAcceleration);
        }

        _desiredAcceleration =
            horizontalAcceleration;
        
        /*_desiredAcceleration =
            horizontalAcceleration +
            Vector3.up * verticalAcceleration;*/
    }

    private void ComputeCruiseAcceleration()
    {
        Vector3 toTarget =
            TargetPosition - _tr.position;

        Vector3 horizontalToTarget =
            Vector3.ProjectOnPlane(
                toTarget,
                Vector3.up);

        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(
                _velocity,
                Vector3.up);

        float horizontalDistance =
            horizontalToTarget.magnitude;

        Vector3 travelDirection;

        if (horizontalDistance > 0.001f)
        {
            travelDirection =
                horizontalToTarget / horizontalDistance;
        }
        else if (horizontalVelocity.sqrMagnitude > 0.001f)
        {
            travelDirection =
                horizontalVelocity.normalized;
        }
        else
        {
            travelDirection =
                Vector3.ProjectOnPlane(
                    _tr.forward,
                    Vector3.up).normalized;
        }

        /*
         * Approach the hover-capture radius at HoverEntrySpeed.
         *
         * Current allowed speed:
         *
         * v� = vFinal� + 2ad
         */
        float brakingDistance = Mathf.Max(
            0f,
            horizontalDistance -
            HoverCaptureRadius);

        float stoppingLimitedSpeed = Mathf.Sqrt(
            HoverEntrySpeed * HoverEntrySpeed +
            2f *
            Mathf.Max(0.1f, ApproachBrakingAcceleration) *
            brakingDistance);

        float desiredSpeed = Mathf.Min(
            MaxSpeed,
            stoppingLimitedSpeed);

        if (ActivePath != null)
            desiredSpeed = MaxSpeed;

        Vector3 desiredHorizontalVelocity =
            travelDirection * desiredSpeed;

        bool braking =
            Vector3.Dot(
                horizontalVelocity,
                travelDirection) >
            desiredSpeed;

        float velocityResponseTime =
            braking
                ? ApproachVelocityResponseTime
                : CruiseVelocityResponseTime;

        velocityResponseTime =
            Mathf.Max(
                0.05f,
                velocityResponseTime);

        Vector3 horizontalAcceleration =
            (desiredHorizontalVelocity -
             horizontalVelocity) /
            velocityResponseTime;

        float accelerationLimit =
            braking
                ? ApproachBrakingAcceleration
                : MaxAcceleration;

        horizontalAcceleration =
            Vector3.ClampMagnitude(
                horizontalAcceleration,
                accelerationLimit);

        float desiredVerticalVelocity =
            Mathf.Clamp(
                toTarget.y /
                CruiseVelocityResponseTime,
                -MaxSpeed * 0.2f,
                 MaxSpeed * 0.2f);

        float verticalAcceleration =
            (desiredVerticalVelocity -
             _velocity.y) /
            Mathf.Max(
                0.05f,
                CruiseVelocityResponseTime);

        verticalAcceleration = Mathf.Clamp(
            verticalAcceleration,
            -MaxAcceleration,
            MaxAcceleration);

        _desiredAcceleration =
            horizontalAcceleration +
            Vector3.up * verticalAcceleration;

        _predictedPosition =
            _tr.position +
            _velocity * PredictionTime;
    }

    private void ComputeHoverAcceleration(Vector3 positionError)
    {
        Vector3 horizontalError =
            Vector3.ProjectOnPlane(
                positionError,
                Vector3.up);

        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(
                _velocity,
                Vector3.up);

        /*
         * Position correction plus strong velocity cancellation.
         *
         * Velocity damping is especially important during capture because the
         * helicopter may enter hover mode with substantial tangential velocity.
         */
        Vector3 horizontalAcceleration =
            horizontalError * HoverPositionGain -
            horizontalVelocity * HoverVelocityGain;

        horizontalAcceleration =
            Vector3.ClampMagnitude(
                horizontalAcceleration,
                MaxHoverAcceleration);

        /*
         * Control altitude separately. This prevents a large vertical error from
         * consuming the acceleration budget needed to stop horizontal motion.
         */
        float verticalAcceleration =
            positionError.y * HoverPositionGain -
            _velocity.y * HoverVelocityGain;

        verticalAcceleration = Mathf.Clamp(
            verticalAcceleration,
            -MaxHoverAcceleration,
            MaxHoverAcceleration);

        if (horizontalError.magnitude <= HoverDeadZone &&
            horizontalVelocity.magnitude <= HoverVelocityDeadZone)
        {
            horizontalAcceleration = Vector3.zero;
        }

        if (Mathf.Abs(positionError.y) <= HoverDeadZone &&
            Mathf.Abs(_velocity.y) <= HoverVelocityDeadZone)
        {
            verticalAcceleration = 0f;
        }

        _desiredAcceleration =
            horizontalAcceleration +
            Vector3.up * verticalAcceleration;
    }

    private void ComputeDesiredOrientation()
    {
        Vector3 desiredHeading;

        if (_hoverMode)
        {
            desiredHeading = _hoverHeading;
        }
        else
        {
            desiredHeading =
                TargetPosition - _tr.position;

            desiredHeading.y = 0f;

            if (desiredHeading.sqrMagnitude < 0.001f)
            {
                desiredHeading =
                    Vector3.ProjectOnPlane(
                        _tr.forward,
                        Vector3.up);
            }
        }

        if (desiredHeading.sqrMagnitude < 0.001f)
            desiredHeading = Vector3.forward;

        desiredHeading.Normalize();

        Vector3 headingRight =
            Vector3.Cross(
                Vector3.up,
                desiredHeading).normalized;

        float forwardAcceleration =
            Vector3.Dot(
                _desiredAcceleration,
                desiredHeading);

        float lateralAcceleration =
            Vector3.Dot(
                _desiredAcceleration,
                headingRight);

        float verticalAcceleration =
            _desiredAcceleration.y;

        /*
         * Reserve vertical thrust first.
         */
        float requiredVerticalThrust =
            Mathf.Max(
                0.01f,
                Gravity + verticalAcceleration);

        /*
         * Limit horizontal acceleration by allowable pitch and roll while
         * preserving the requested vertical thrust.
         */
        float maxForwardFromPitch =
            requiredVerticalThrust *
            Mathf.Tan(
                Mathf.Clamp(MaxPitch, 0f, 80f) *
                Mathf.Deg2Rad);

        float maxLateralFromRoll =
            requiredVerticalThrust *
            Mathf.Tan(
                Mathf.Clamp(MaxRoll, 0f, 80f) *
                Mathf.Deg2Rad);

        forwardAcceleration = Mathf.Clamp(
            forwardAcceleration,
            -maxForwardFromPitch,
            maxForwardFromPitch);

        lateralAcceleration = Mathf.Clamp(
            lateralAcceleration,
            -maxLateralFromRoll,
            maxLateralFromRoll);

        /*
         * Ensure the combined vertical and horizontal thrust demand cannot
         * exceed MaxLift.
         */
        float maxHorizontalFromLift =
            Mathf.Sqrt(
                Mathf.Max(
                    0f,
                    MaxLift * MaxLift -
                    requiredVerticalThrust *
                    requiredVerticalThrust));

        Vector2 horizontalRequest =
            new Vector2(
                forwardAcceleration,
                lateralAcceleration);

        if (horizontalRequest.magnitude > maxHorizontalFromLift)
        {
            horizontalRequest =
                horizontalRequest.normalized *
                maxHorizontalFromLift;

            forwardAcceleration =
                horizontalRequest.x;

            lateralAcceleration =
                horizontalRequest.y;
        }

        _desiredAcceleration =
            desiredHeading * forwardAcceleration +
            headingRight * lateralAcceleration;

        Vector3 requiredThrust =
            _desiredAcceleration +
            Vector3.up * Gravity;

        if (requiredThrust.sqrMagnitude < 0.0001f)
            requiredThrust = Vector3.up * Gravity;

        _desiredUp =
            requiredThrust.normalized;

        _desiredForward =
            Vector3.ProjectOnPlane(
                desiredHeading,
                _desiredUp);

        if (_desiredForward.sqrMagnitude < 0.001f)
        {
            _desiredForward =
                Vector3.ProjectOnPlane(
                    _tr.forward,
                    _desiredUp);
        }

        if (_desiredForward.sqrMagnitude < 0.001f)
        {
            _desiredForward =
                Vector3.Cross(
                    _tr.right,
                    _desiredUp);
        }

        _desiredForward.Normalize();
    }

    /// <summary>
    /// Returns true if we're close enough to enter hover mode.
    /// </summary>
    /*private bool ShouldHover()
    {
        float dist =
            Vector3.Distance(_tr.position, TargetPosition);

        return dist < HoverRadius;
    }*/

    /// <summary>
    /// Incrementally steers the helicopter toward the desired lift vector
    /// and desired forward vector.
    /// Inspired by CBaseHelicopter::Flight().
    /// </summary>
    private void UpdateAngularMotion(float dt)
    {
        Quaternion desiredRotation =
            Quaternion.LookRotation(_desiredForward, _desiredUp);

        Quaternion errorRotation =
            Quaternion.Inverse(_tr.rotation) * desiredRotation;

        errorRotation.ToAngleAxis(
            out float errorAngle,
            out Vector3 localErrorAxis);

        if (errorAngle > 180f)
            errorAngle -= 360f;

        Vector3 desiredLocalAngularVelocity = Vector3.zero;

        if (Mathf.Abs(errorAngle) > 0.001f &&
            localErrorAxis.sqrMagnitude > 0.000001f)
        {
            localErrorAxis.Normalize();

            float responseTime =
                Mathf.Max(0.05f, AttitudeResponseTime);//Mathf.Max(0.05f, PredictionTime);

            desiredLocalAngularVelocity =
                localErrorAxis * (errorAngle / responseTime);
        }

        Vector3 angularAcceleration =
            (desiredLocalAngularVelocity - _angularVelocity) /
            Mathf.Max(dt, 0.0001f);

        angularAcceleration.x = Mathf.Clamp(
            angularAcceleration.x,
            -PitchAcceleration,
            PitchAcceleration);

        angularAcceleration.y = Mathf.Clamp(
            angularAcceleration.y,
            -YawAcceleration,
            YawAcceleration);

        angularAcceleration.z = Mathf.Clamp(
            angularAcceleration.z,
            -RollAcceleration,
            RollAcceleration);

        _angularVelocity += angularAcceleration * dt;

        _angularVelocity.y = Mathf.Clamp(
            _angularVelocity.y,
            -MaxYawRate,
            MaxYawRate);

        Quaternion angularStep =
            Quaternion.Euler(_angularVelocity * dt);

        // Angular velocity is local, so post-multiply.
        _tr.rotation = _tr.rotation * angularStep;
    }
    
    //RotorAnimator.SetFloat("Speed", (1.5f * targetLift + 5) / MaxLift);

    private void UpdateLift(float dt)
    {
        float altitudeError =
            TargetPosition.y - _tr.position.y;

        /*
         * This is NOT another PID.
         *
         * It's simply Newton's second law:
         *
         * Need enough lift to:
         *
         * - cancel gravity
         * - stop current vertical motion
         * - remove altitude error
         */

        float desiredVerticalAcceleration =
            altitudeError
            - _velocity.y;

        float targetLift =
            Gravity +
            desiredVerticalAcceleration;

        /*
         * Compensate for current attitude.
         */

        float verticalFraction =
            Mathf.Max(
                0.2f,
                Vector3.Dot(
                    _tr.up,
                    Vector3.up));

        targetLift /= verticalFraction;

        targetLift =
            Mathf.Clamp(
                targetLift,
                0f,
                MaxLift);

        RotorAnimator.SetFloat(
            "Speed",
            (1.5f * targetLift + 5) /
            MaxLift);

        _lift =
            Mathf.MoveTowards(
                _lift,
                targetLift,
                LiftResponse * dt);
    }

    /*private void ApplyForces(float dt)
    {
        Vector3 liftAcceleration = _tr.up * _lift;
        Vector3 gravityAcceleration = Vector3.down * Gravity;

        _velocity +=
            (liftAcceleration + gravityAcceleration) * dt;
    }*/
    
    private void ApplyForces(float dt)
    {
        _velocity +=
            (_tr.up * _lift -
             Vector3.up * Gravity)
            * dt;
    }

    private void ApplyDrag(float dt)
    {
        Vector3 forward =
            Vector3.Project(_velocity, _tr.forward);

        Vector3 right =
            Vector3.Project(_velocity, _tr.right);

        Vector3 up =
            Vector3.Project(_velocity, _tr.up);

        forward *= Mathf.Clamp01(1f - ForwardDrag * dt);

        right *= Mathf.Clamp01(1f - SideDrag * dt);

        up *= Mathf.Clamp01(1f - VerticalDrag * dt);

        _velocity = forward + right + up;
    }

    private void Integrate(float dt)
    {
        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(
                _velocity,
                Vector3.up);

        float horizontalSpeed =
            horizontalVelocity.magnitude;

        if (horizontalSpeed > MaxSpeed)
        {
            horizontalVelocity *=
                MaxSpeed / horizontalSpeed;

            _velocity =
                horizontalVelocity +
                Vector3.up * _velocity.y;
        }

        _tr.position += _velocity * dt;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(TargetPosition, 1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_predictedPosition, 0.5f);
    }
}
