using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class LaserPointer : MonoBehaviour
{
    public XRRayInteractor ray;
    public LineRenderer line;
    public Transform pointerDot;
    public float maxDistance = 10f;
    
    private Vector3 smoothedHit;
    public float smoothSpeed = 20f; // higher = faster snap, lower = smoother

    void Awake()
    {
        if (ray == null) ray = GetComponent<XRRayInteractor>();
        if (line == null) line = GetComponent<LineRenderer>();

        // Prevent billboard “flat plane” look
        line.alignment = LineAlignment.TransformZ;
        line.startWidth = 0.002f;
        line.endWidth = 0.002f;
    }

    void Update()
    {
        if (ray == null || line == null)
            return;

        Vector3 start = ray.transform.position;

        if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            Vector3 rawHit = hit.point;

            // Smooth target point
            if (smoothedHit == Vector3.zero)
                smoothedHit = rawHit;

            smoothedHit = Vector3.Lerp(smoothedHit, rawHit, Time.deltaTime * smoothSpeed);

            // Update line
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, smoothedHit);

            if (pointerDot != null)
            {
                pointerDot.gameObject.SetActive(true);

                // Offset dot ABOVE UI surface
                pointerDot.position = smoothedHit + hit.normal * 0.003f;
            }
        }
        else
        {
            Vector3 end = start + ray.transform.forward * maxDistance;

            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);

            if (pointerDot != null)
                pointerDot.gameObject.SetActive(false);
        }
    }
}