using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeVisual : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;

    public float Width = 0.02f;

    public Color RopeColor = Color.black;

    public int Segments = 8;

    private LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        // Add a LineRenderer component
        lineRenderer = gameObject.AddComponent<LineRenderer>();

        // Set the material
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Set the color
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;

        // Set the width
        lineRenderer.startWidth = Width;
        lineRenderer.endWidth = Width;

        // Set the number of vertices
        lineRenderer.positionCount = Segments;
    }

    // Update is called once per frame
    void Update()
    {
        if (lineRenderer == null || startPoint == null || endPoint == null)
            return;

        for (int i = 0; i < Segments; i++)
        {
            float t = i / (Segments - 1);
            Vector3 position = Vector3.Lerp(startPoint.position, endPoint.position, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.1f; // Add a sine wave for a rope-like effect
            position.x += Mathf.Sin(t * Mathf.PI * 2f) * 0.1f; // Add a small horizontal wave for more realism
            position.z += Mathf.Sin(t * Mathf.PI * 4f) * 0.05f; // Add a small depth wave for more realism
            lineRenderer.SetPosition(i, position);
        }
    }
}
