using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class LaserPointer : MonoBehaviour
{
    #region Inspector Stuff (Ray Visuals)
    [SerializeField]
    private XRRayInteractor ray;

    [SerializeField]
    private LineRenderer line;

    [SerializeField]
    private Transform pointerDot; // Visual indicator only, kept in Inspector for UI placement

    [SerializeField]
    private float maxDistance = 10f;

    [SerializeField]
    private float smoothSpeed = 20f; // higher = faster snap, lower = smoother
    #endregion
    
    #region Current State (What Is Happening Right Now)
    private Vector3 smoothedHit;
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    private void Awake()
    {
        ray ??= GetComponent<XRRayInteractor>();
        line ??= GetComponent<LineRenderer>();

        // Prevent billboard “flat plane” look
        line.alignment = LineAlignment.TransformZ;
        line.startWidth = 0.002f;
        line.endWidth = 0.002f;
    }
    #endregion

    #region Main Logic (What Actually Happens)
    private void Update()
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
    #endregion
}
