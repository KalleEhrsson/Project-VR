using UnityEngine;

public class GrabPoint : MonoBehaviour
{
    #region Inspector Stuff (Grab Configuration)
    [SerializeField]
    private int priority = 10;

    [Tooltip("Explicit transform that defines the grab pose for this point. Defaults to this transform.")]
    [SerializeField]
    private Transform attachTransform; // Kept serialized to allow designers to override the grab pose visually
    #endregion

    #region Cached Components (Self Setup)
    private Rigidbody owningRigidbody;
    #endregion

    #region Current State (What Is Happening Right Now)
    private static GameObject bubblePrefab;
    private GameObject bubbleInstance;
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    private void Awake()
    {
        owningRigidbody = GetComponentInParent<Rigidbody>();
        if (attachTransform == null)
            attachTransform = transform;

        if (bubblePrefab == null)
            bubblePrefab = Resources.Load<GameObject>("GrabBubble");

        if (owningRigidbody == null)
            Debug.LogWarning($"GrabPoint on {name} has no parent Rigidbody. This point will be ignored at runtime.");
    }
    #endregion

    #region Current State (Public Accessors)
    public bool IsAboveFloor()
    {
        return attachTransform.position.y >= TapFloorCalibrator.RealFloorY + 0.02f;
    }

    public int Priority => priority;

    public Rigidbody AttachedRigidbody => owningRigidbody;
    #endregion

    #region Main Logic (What Actually Happens)
    public Pose GetAttachPose()
    {
        if (attachTransform == null)
            attachTransform = transform;

        return new Pose(attachTransform.position, attachTransform.rotation);
    }

    public void Show()
    {
        if (!IsAboveFloor())
            return;

        if (bubblePrefab == null)
        {
            Debug.LogError("GrabBubble prefab not found in Resources folder");
            return;
        }

        if (bubbleInstance == null)
        {
            bubbleInstance = Instantiate(bubblePrefab, transform);
            bubbleInstance.transform.localPosition = Vector3.zero;
            bubbleInstance.transform.localRotation = Quaternion.identity;

            Vector3 prefabScale = bubblePrefab.transform.localScale;
            Vector3 lossy = transform.lossyScale;
            float uniformParentScale = Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));
            if (Mathf.Approximately(uniformParentScale, 0f))
                uniformParentScale = 1f;

            bubbleInstance.transform.localScale = prefabScale / uniformParentScale;
        }

        bubbleInstance.SetActive(true);
    }

    public void Hide()
    {
        if (bubbleInstance != null)
            bubbleInstance.SetActive(false);
    }

    public Vector3 GetLocalAttachPoint(Transform root)
    {
        return root.InverseTransformPoint(GetAttachPose().position);
    }

    public Quaternion GetLocalAttachRotation(Transform root)
    {
        return Quaternion.Inverse(root.rotation) * GetAttachPose().rotation;
    }
    #endregion
}
