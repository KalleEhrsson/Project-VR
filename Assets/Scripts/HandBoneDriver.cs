using UnityEngine;
using UnityEngine.XR;

public class HandBoneDriver : MonoBehaviour
{
    #region Inspector Stuff (Hand Configuration)
    [Tooltip("Defines which controller drives the bone naming prefix for auto-assign.")]
    [SerializeField]
    private XRNode handNode;

    [System.Serializable]
    public class FingerBones
    {
        [ReadOnly]
        public Transform bone1;

        [ReadOnly]
        public Transform bone2;

        [ReadOnly]
        public Transform bone3;
    }

    [SerializeField]
    private FingerBones index;

    [SerializeField]
    private FingerBones middle;

    [SerializeField]
    private FingerBones ring;

    [SerializeField]
    private FingerBones little;

    [SerializeField]
    private FingerBones thumb;

    [Header("Settings")]
    [SerializeField]
    private float fingerCurl = 70f;

    [SerializeField]
    private float thumbCurl = 60f;
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    private void Awake()
    {
        AutoAssign();
    }
    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-assign in edit mode too
        if (!Application.isPlaying)
            AutoAssign();
    }
#endif

    #region Main Logic (What Actually Happens)
    private void AutoAssign()
    {
        string prefix = (handNode == XRNode.LeftHand) ? "L_" : "R_";

        AssignFinger(index, prefix, "Index");
        AssignFinger(middle, prefix, "Middle");
        AssignFinger(ring, prefix, "Ring");
        AssignFinger(little, prefix, "Little");

        thumb.bone1 = FindBone(prefix + "ThumbProximal");
        thumb.bone2 = FindBone(prefix + "ThumbDistal");
        thumb.bone3 = FindBone(prefix + "ThumbTip");

        ValidateAssignments(prefix);
    }

    private void AssignFinger(FingerBones f, string prefix, string name)
    {
        f.bone1 = FindBone(prefix + name + "Proximal");
        f.bone2 = FindBone(prefix + name + "Intermediate");
        f.bone3 = FindBone(prefix + name + "Distal");
    }

    private Transform FindBone(string name)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    private void ValidateAssignments(string prefix)
    {
        ValidateFinger(index, prefix, "Index");
        ValidateFinger(middle, prefix, "Middle");
        ValidateFinger(ring, prefix, "Ring");
        ValidateFinger(little, prefix, "Little");
        ValidateFinger(thumb, prefix, "Thumb");
    }

    private void ValidateFinger(FingerBones f, string prefix, string fingerName)
    {
        if (f.bone1 == null || f.bone2 == null || f.bone3 == null)
        {
            Debug.LogWarning(
                $"HandBoneDriver on {gameObject.name} is missing bones for {prefix}{fingerName}. Check rig naming or hierarchy.");
        }
    }

    private void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(handNode);
        device.TryGetFeatureValue(CommonUsages.grip, out float grip);
        device.TryGetFeatureValue(CommonUsages.trigger, out float trigger);

        CurlFinger(index, trigger * fingerCurl);
        CurlFinger(middle, grip * fingerCurl);

        CurlFinger(ring, grip * fingerCurl * 1.1f);
        CurlFinger(little, grip * fingerCurl * 1.2f);

        CurlFinger(thumb, grip * thumbCurl);
    }

    private void CurlFinger(FingerBones f, float curl)
    {
        // how much each joint bends
        float p = curl * 0.55f; // proximal
        float m = curl * 0.35f; // middle
        float d = curl * 0.15f; // distal
    
        // inward wrap angle (fingers rotate a bit sideways)
        float wrap = curl * 0.25f;
    
        if (f.bone1)
            f.bone1.localRotation = Quaternion.Euler(p, wrap, 0);
    
        if (f.bone2)
            f.bone2.localRotation = Quaternion.Euler(m, wrap * 0.5f, 0);
    
        if (f.bone3)
            f.bone3.localRotation = Quaternion.Euler(d, wrap * 0.2f, 0);
    }
    #endregion
}

public class ReadOnlyAttribute : PropertyAttribute {}
