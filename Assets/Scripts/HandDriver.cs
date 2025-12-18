using UnityEngine;
using UnityEngine.InputSystem;

public class HandDriver : MonoBehaviour
{
    #region Inspector Stuff (Input Actions)
    [SerializeField]
    private InputActionProperty positionAction; // Kept serialized because action names vary per controller layout

    [SerializeField]
    private InputActionProperty rotationAction; // Kept serialized because action names vary per controller layout
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
