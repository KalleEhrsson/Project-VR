using UnityEngine;
using UnityEngine.InputSystem;

public class HandDriver : MonoBehaviour
{
    #region Inspector Stuff (Input Actions)
    [Tooltip("Input action that drives this hand's local position. Must be assigned per input setup.")]
    [SerializeField]
    private InputActionProperty positionAction; // Kept serialized because action names vary per controller layout

    [Tooltip("Input action that drives this hand's local rotation. Must be assigned per input setup.")]
    [SerializeField]
    private InputActionProperty rotationAction; // Kept serialized because action names vary per controller layout
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    private void Awake()
    {
        if (positionAction.action == null || rotationAction.action == null)
        {
            Debug.LogWarning($"HandDriver on {name} is missing input actions. Hand tracking will be idle.");
        }
    }
    #endregion

    #region Main Logic (What Actually Happens)
    private void Update()
    {
        Vector3 pos = positionAction.action != null
            ? positionAction.action.ReadValue<Vector3>()
            : Vector3.zero;

        Quaternion rot = rotationAction.action != null
            ? rotationAction.action.ReadValue<Quaternion>()
            : Quaternion.identity;

        transform.localPosition = pos;
        transform.localRotation = rot;
    }
    #endregion
}
