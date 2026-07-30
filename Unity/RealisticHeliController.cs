using System;
using UnityEngine;

/// <summary>
/// Yes I used AI for this. As I'm writing this AI is trying to make me sound smart and I hate it.
/// It's literally trying to put words in my mouth.
/// I tried making a controller from scratch but I couldn't get the floatiness that I wanted so I went down this route.
/// The other option was making a whole ass physics based controller and using PID controllers for autopilot but that seemed excessive.
/// </summary>
public class RealisticHeliController : MonoBehaviour
{
    [Header("Path")]
    public HeliPath Path;
    public int CurrentNodeIndex;

    [Header("Animation")]
    public Animator RotorAnimator;

    [Header("Movement")]
    public float MaxSpeed = 80f;
    public float MaxAcceleration = 24f;

    [Header("Guidance")]
    public float LookAheadMin = 15f;
    public float LookAheadMax = 70f;
    public float CrossTrackGain = 1.8f;

    [Header("Yaw Dynamics")]
    public float MaxYawRate = 90f;
    public float YawResponsiveness = 5.5f;
    public float YawAcceleration = 220f;

    [Header("Bank")]
    public float MaxBank = 28f;
    public float BankSpring = 7f;
    public float BankDamping = 5f;

    [Header("Pitch")]
    public float MaxPitch = 12f;
    public float PitchSpring = 6f;
    public float PitchDamping = 5f;
    public float CollectivePitchGain = 6f;

    [Header("Altitude")]
    public float AltitudeGain = 2.5f;
    public float MaxVerticalSpeed = 12f;

    [Header("Hover")]
    public float HoverPositionTolerance = 0.5f;
    public float JitterYScale = 0.3f;
    public float JitterYFrequency = 0.3f;
    public float JitterSpeedScale = 1.5f;

    [Header("Visuals")]
    public float GustScale = 1f;
    public float FlapScale = 2f;
    public float RotorShakeScale = 1f;
    public float WobbleScale = 1f;
    public float WindScale = 1f;

    public Transform VisualChild;

    public Transform GunnerLeft;
    public Transform GunnerRight;
    public Transform GunLeft;
    public Transform GunRight;
    
    Vector3 _velocity;

    float _yaw;
    float _yawRate;

    float _bank;
    float _bankVel;

    float _pitch;
    float _pitchVel;

    float _verticalSpeed;

    Vector3 _wind;

    bool _reachedHover = false;

    // Events
    public delegate void NodeReachedHandler(int nodeIndex);
    public delegate void PathCompletedHandler();
    public delegate void StartedHover();

    public event NodeReachedHandler OnNodeReached;
    public event PathCompletedHandler OnPathCompleted;
    public event StartedHover OnStartedHover;

    private int _lastReachedNodeIndex = -1;

    // =========================================================
    // HOVERING PUBLIC API
    // =========================================================
    bool _isHovering;
    Vector3 _hoverPosition;
    float _hoverAltitude;

    public void StartHover(Vector3 hoverPoint, float hoverAltitude)
    {
        _isHovering = true;
        _reachedHover = false;
        _hoverPosition = new Vector3(hoverPoint.x, 0, hoverPoint.z);
        _hoverAltitude = hoverAltitude;
    }

    public void StartHoverInPlace()
    {
        _isHovering = true;
        _reachedHover = false;
        Vector3 currentPos = transform.position;
        _hoverPosition = new Vector3(currentPos.x, 0, currentPos.z);
        _hoverAltitude = currentPos.y;
    }

    public void StopHover()
    {
        _isHovering = false;
        _reachedHover = false;
    }

    public bool IsHovering => _isHovering;

    // =========================================================
    // HOVER GUIDANCE
    // =========================================================
    void GetHoverGuidance(out Vector3 desiredDir, out float desiredSpeed, out float verticalTarget)
    {
        Vector3 p = transform.position;
        Vector3 toHover = new Vector3(_hoverPosition.x, 0, _hoverPosition.z) - new Vector3(p.x, 0, p.z);
        float horizontalDist = toHover.magnitude;

        if (horizontalDist > HoverPositionTolerance)
        {
            desiredDir = toHover.normalized;
            desiredSpeed = Mathf.Min(horizontalDist * 0.5f, MaxSpeed);
        }
        else
        {
            if (!_reachedHover)
            {
                _reachedHover = true;
                OnStartedHover?.Invoke();
            }

            desiredDir = Vector3.zero;
            desiredSpeed = 0;
        }

        verticalTarget = _hoverAltitude + Noise(Time.time * JitterYFrequency, 35) * JitterYScale;
    }

    void Start()
    {
        _yaw = transform.eulerAngles.y;

        if (Path == null)
        {
            StartHoverInPlace();
            //StartHover(transform.position + new Vector3(-300, 0, 0), 100);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        UpdateWind();

        if (_isHovering)
        {
            GetHoverGuidance(out Vector3 desiredDir, out float speedTarget, out float verticalTarget);
            StepYaw(dt, desiredDir);
            StepMotion(dt, desiredDir, speedTarget);
            StepAltitude(dt, verticalTarget);
            StepAttitude(dt, desiredDir, speedTarget, verticalTarget);
        }
        else if (Path != null && Path.Nodes.Count >= 2)
        {
            //StepGuidance(out Vector3 desiredDir, out float speedTarget, out float verticalTarget);
            GetGuidance(out Vector3 desiredDir, out float speedTarget, out float verticalTarget);
            StepYaw(dt, desiredDir);
            StepMotion(dt, desiredDir, speedTarget);
            StepAltitude(dt, verticalTarget);
            StepAttitude(dt, desiredDir, speedTarget, verticalTarget);
        }

        ApplyTransform();
    }

    Vector3 ClosestPointOnSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Vector3.Dot(p - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    bool HasPassedVertex(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = (b - a).normalized;
        Vector3 ap = p - a;

        // positive means you're past the plane perpendicular to segment
        return Vector3.Dot(ap, ab) > Vector3.Distance(a, b);
    }

    void UpdateWind()
    {
        Vector3 desiredWind = new Vector3(
            Noise(Time.time * 0.05f, 1),
            Noise(Time.time * 0.07f, 2),
            Noise(Time.time * 0.06f, 3)
        );

        _wind = Vector3.Lerp(
            _wind,
            desiredWind * (2.0f * WindScale),
            1f - Mathf.Exp(-0.5f * Time.deltaTime));
    }

    // =========================================================
    // GUIDANCE (CORRIDOR TRACKING + LOOKAHEAD)
    // =========================================================
    void GetGuidance(out Vector3 desiredDir, out float desiredSpeed, out float verticalTarget)
    {
        Vector3 p = transform.position;

        // ---------------------------------------------------
        // 1. FIND BEST SEGMENT (unchanged, but stable)
        // ---------------------------------------------------
        float bestDist = float.MaxValue;
        /*int bestIndex = CurrentNodeIndex;

        for (int i = 0; i < Path.Nodes.Count; i++)
        {
            Vector3 a = Path.Nodes[i].transform.position;
            Vector3 b = Path.Nodes[(i + 1) % Path.Nodes.Count].transform.position;

            Vector3 closest = ClosestPointOnSegment(p, a, b);
            float d = (p - closest).sqrMagnitude;

            if (d < bestDist)
            {
                bestDist = d;
                bestIndex = i;
            }
        }*/

        int bestIndex = CurrentNodeIndex;

        // ONLY evaluate current and next segment
        for (int nOffset = 0; nOffset <= 1; nOffset++)
        {
            int i = (CurrentNodeIndex + nOffset) % Path.Nodes.Count;

            Vector3 a = Path.Nodes[i].transform.position;
            Vector3 b = Path.Nodes[(i + 1) % Path.Nodes.Count].transform.position;

            Vector3 closest = ClosestPointOnSegment(p, a, b);
            float d = (p - closest).sqrMagnitude;

            if (d < float.MaxValue)
            {
                if (nOffset == 0 || HasPassedVertex(a, b, p))
                {
                    bestIndex = i;
                    break;
                }
            }
        }

        if (CurrentNodeIndex != bestIndex)
        {
            if (Vector3.Distance(p, Path.Nodes[CurrentNodeIndex].transform.position) < 8f)
            {
                CurrentNodeIndex = bestIndex;
            }
        }

        //CurrentNodeIndex = bestIndex;

        // ---------------------------------------------------
        // 2. SEGMENT DATA
        // ---------------------------------------------------
        Vector3 A = Path.Nodes[CurrentNodeIndex].transform.position;
        Vector3 B = Path.Nodes[(CurrentNodeIndex + 1) % Path.Nodes.Count].transform.position;

        Vector3 segment = (B - A);
        float segLen = segment.magnitude;
        Vector3 dir = segment / segLen;

        Vector3 closestPoint = ClosestPointOnSegment(p, A, B);

        // ---------------------------------------------------
        // 3. STABLE CROSS-TRACK CORRECTION (FIXED)
        //    (no more distance-squared blowups)
        // ---------------------------------------------------
        Vector3 offset = p - closestPoint;

        /*Vector3 lateralDir = Vector3.Cross(Vector3.up, dir).normalized;

        float crossError = Vector3.Dot(offset, lateralDir);

        // NORMALIZED correction (critical fix)
        float crossNormalized =
            Mathf.Clamp(crossError / 10f, -1f, 1f);

        Vector3 lateralCorrection =
            -lateralDir * crossNormalized * 12f;*/

        // ---------------------------------------------------
        // 4. LOOKAHEAD (bounded + segment aware)
        // ---------------------------------------------------
        float speed01 =
            Mathf.InverseLerp(0f, MaxSpeed, _velocity.magnitude);

        float lookAhead =
            Mathf.Lerp(10f, 35f, speed01);

        // clamp so it NEVER leaves segment space
        lookAhead = Mathf.Min(lookAhead, segLen);

        Vector3 forwardPoint =
            closestPoint + dir * lookAhead;


        // ---------------------------------------------------
        // STABILITY LAYER (this is the missing piece)
        // ---------------------------------------------------
        Vector3 lateralDir = Vector3.Cross(Vector3.up, dir);

        float lateralSpeed = Vector3.Dot(_velocity, lateralDir);

        // velocity damping toward path
        Vector3 lateralDamping =
            -lateralDir * (lateralSpeed * 0.8f);

        // cross-track correction (saturated)
        float crossError = Vector3.Dot(transform.position - closestPoint, lateralDir);

        float crossCorrection =
            (float)Math.Tanh(crossError * 0.25f) * 8f;

        Vector3 stability =
            lateralDamping + (-lateralDir * crossCorrection);

        // ---------------------------------------------------
        // 5. GUIDANCE BLEND (FIXED NORMALIZATION BUG)
        // ---------------------------------------------------
        Vector3 rawTarget =
            forwardPoint + stability;

        Vector3 toTarget =
            rawTarget - p;

        float distance = toTarget.magnitude;

        // soft normalization prevents runaway gain at long distances
        desiredDir =
            toTarget / (distance + 0.001f);

        //float speed01 = _velocity.magnitude / MaxSpeed;

        float wobble =
            Noise(Time.time * 0.6f, 50)
            * Mathf.Lerp(0f, 0.6f, speed01) * WobbleScale;

        desiredDir = Vector3.Normalize(desiredDir + _wind + wobble * Vector3.up);

        // ---------------------------------------------------
        // 6. NODE PROGRESSION (no radius, fully deterministic)
        // ---------------------------------------------------
        float t = Vector3.Dot(p - A, dir);

        if (t > segLen + 5f)
        {
            CurrentNodeIndex = (CurrentNodeIndex + 1) % Path.Nodes.Count;
        }

        // ---------------------------------------------------
        // 6b. NODE ARRIVAL DETECTION + EVENTS
        // ---------------------------------------------------
        float distToCurrentNode = Vector3.Distance(p, Path.Nodes[CurrentNodeIndex].transform.position);
        if (distToCurrentNode < 20f)
        {
            if (_lastReachedNodeIndex != CurrentNodeIndex)
            {
                _lastReachedNodeIndex = CurrentNodeIndex;
                OnNodeReached?.Invoke(CurrentNodeIndex);

                // Check if we've completed the path (looped back to start)
                if (CurrentNodeIndex == 0 && _lastReachedNodeIndex > 0)
                {
                    OnPathCompleted?.Invoke();
                }
            }
        }

        // ---------------------------------------------------
        // 7. SPEED + ALTITUDE
        // ---------------------------------------------------
        desiredSpeed =
            Path.Nodes[CurrentNodeIndex].DesiredSpeed;

        verticalTarget =
            Path.Nodes[CurrentNodeIndex].transform.position.y;
    }

    // =========================================================
    // YAW (INERTIAL TURN MODEL)
    // =========================================================
    void StepYaw(float dt, Vector3 desiredDir)
    {
        desiredDir.y = 0;
        if (desiredDir.sqrMagnitude < 0.001f)
            return;

        float desiredYaw =
            Quaternion.LookRotation(desiredDir).eulerAngles.y;

        float yawError =
            Mathf.DeltaAngle(_yaw, desiredYaw);

        float desiredYawRate =
            Mathf.Clamp(yawError * YawResponsiveness,
                -MaxYawRate,
                MaxYawRate);

        float yawAccel =
            (desiredYawRate - _yawRate) * YawAcceleration;

        yawAccel = Mathf.Clamp(yawAccel, -YawAcceleration, YawAcceleration);

        _yawRate += yawAccel * dt;
        _yawRate = Mathf.Clamp(_yawRate, -MaxYawRate, MaxYawRate);

        _yaw += _yawRate * dt;
    }

    // =========================================================
    // FORWARD MOTION (NO SLIP, BUT RESPONSIVE CORRECTION)
    // =========================================================
    void StepMotion(float dt, Vector3 desiredDir, float speedTarget)
    {
        Vector3 desiredVelocity =
            desiredDir * speedTarget;

        Vector3 accel =
            (desiredVelocity - _velocity) * 3.8f;

        accel = Vector3.ClampMagnitude(accel, MaxAcceleration);

        _velocity += accel * dt;

        _velocity = Vector3.ClampMagnitude(_velocity, MaxSpeed);

        // Calculate throttle as combined forward speed + vertical effort
        float horizontalSpeed = new Vector3(_velocity.x, 0, _velocity.z).magnitude;
        float verticalEffort = Mathf.Max(_verticalSpeed + 10f, 10f) / MaxVerticalSpeed;
        var throttle = Mathf.Max(horizontalSpeed / MaxSpeed, verticalEffort);

        RotorAnimator.SetFloat("Speed", throttle);

        transform.position += _velocity * dt;

        float rotorX = Noise(Time.time * 17.2f, 0) * 0.015f * RotorShakeScale;
        float rotorY = Noise(Time.time * 16.7f, 1) * 0.020f * RotorShakeScale;
        float rotorZ = Noise(Time.time * 18.1f, 2) * 0.015f * RotorShakeScale;

        Vector3 rotorShake = new(rotorX, rotorY, rotorZ);

        VisualChild.localPosition = rotorShake;
    }

    // =========================================================
    // ALTITUDE CONTROL
    // =========================================================
    void StepAltitude(float dt, float altTarget)
    {
        float error = altTarget - transform.position.y;

        float desiredVertical =
            Mathf.Clamp(error * AltitudeGain,
                -MaxVerticalSpeed,
                MaxVerticalSpeed);

        _verticalSpeed =
            Mathf.Lerp(_verticalSpeed, desiredVertical, 1f - Mathf.Exp(-3f * dt));

        transform.position += Vector3.up * (_verticalSpeed * dt);
    }

    // =========================================================
    // ATTITUDE (BANK + PITCH FROM REAL DYNAMICS)
    // =========================================================
    void StepAttitude(float dt, Vector3 desiredDir, float speedTarget, float verticalTarget)
    {
        // BANK FROM TURN RATE (stable + no inversion risk)
        float turnFactor =
            Mathf.Clamp(_yawRate / MaxYawRate, -1f, 1f);

        float targetBank =
            -turnFactor * MaxBank;

        float gust =
            Noise(Time.time * 0.9f, 90);

        targetBank += gust * GustScale;

        _bankVel += (targetBank - _bank) * BankSpring * dt;
        _bankVel *= Mathf.Exp(-BankDamping * dt);
        _bank += _bankVel * dt;

        // PITCH (cruise + acceleration feel)
        float speed01 =
            Mathf.InverseLerp(0, MaxSpeed, _velocity.magnitude);

        float cruisePitch =
            Mathf.Lerp(0f, -6f, speed01);

        float accelPitch =
            Vector3.Dot(_velocity.normalized, desiredDir) < 0
                ? 2f
                : 0f;

        float climb01 = Mathf.Clamp(_verticalSpeed / MaxVerticalSpeed, -1f, 1f);

        var climbPitch = climb01 * 10f;

        float altitudeError =
        verticalTarget - transform.position.y;

        float anticipation =
            Mathf.Clamp(altitudeError * 0.12f, -4f, 4f);

        //float altitudeError = verticalTarget - transform.position.y;

        float desiredVerticalSpeed =
            Mathf.Clamp(altitudeError * AltitudeGain,
                        -MaxVerticalSpeed,
                         MaxVerticalSpeed);

        float collectiveCommand =
            desiredVerticalSpeed - _verticalSpeed;

        collectiveCommand /= MaxVerticalSpeed;

        float collectivePitch =
            -collectiveCommand * CollectivePitchGain;

        float targetPitch =
            Mathf.Clamp(cruisePitch + accelPitch + collectivePitch,
                -MaxPitch,
                MaxPitch);

        float flap =
            Noise(Time.time * 0.9f, 80);

        targetPitch += flap * FlapScale;

        _pitchVel += (targetPitch - _pitch) * PitchSpring * dt;
        _pitchVel *= Mathf.Exp(-PitchDamping * dt);
        _pitch += _pitchVel * dt;
    }

    void ApplyTransform()
    {
        transform.rotation =
            Quaternion.Euler(-_pitch, _yaw, _bank);
    }

    float Noise(float x, float seed)
    {
        return Mathf.PerlinNoise(seed, x) * 2f - 1f;
    }
}