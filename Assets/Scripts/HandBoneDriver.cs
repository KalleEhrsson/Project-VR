using UnityEngine;
using UnityEngine.XR;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HandBoneDriver : MonoBehaviour
{
    public XRNode handNode;

    [System.Serializable]
    public class FingerBones
    {
        [ReadOnly] public Transform bone1;
        [ReadOnly] public Transform bone2;
        [ReadOnly] public Transform bone3;
    }

    public FingerBones index;
    public FingerBones middle;
    public FingerBones ring;
    public FingerBones little;
    public FingerBones thumb;

    [Header("Settings")]
    public float fingerCurl = 70f;
    public float thumbCurl = 60f;

    void Awake()
    {
        AutoAssign();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-assign in edit mode too
        if (!Application.isPlaying)
            AutoAssign();
    }
#endif

    void AutoAssign()
    {
        string prefix = (handNode == XRNode.LeftHand) ? "L_" : "R_";

        AssignFinger(index, prefix, "Index");
        AssignFinger(middle, prefix, "Middle");
        AssignFinger(ring, prefix, "Ring");
        AssignFinger(little, prefix, "Little");

        thumb.bone1 = FindBone(prefix + "ThumbProximal");
        thumb.bone2 = FindBone(prefix + "ThumbDistal");
        thumb.bone3 = FindBone(prefix + "ThumbTip");
    }

    void AssignFinger(FingerBones f, string prefix, string name)
    {
        f.bone1 = FindBone(prefix + name + "Proximal");
        f.bone2 = FindBone(prefix + name + "Intermediate");
        f.bone3 = FindBone(prefix + name + "Distal");
    }

    Transform FindBone(string name)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    void Update()
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

    void CurlFinger(FingerBones f, float curl)
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
}

public class ReadOnlyAttribute : PropertyAttribute {}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, true);
        GUI.enabled = true;
    }
}
#endif