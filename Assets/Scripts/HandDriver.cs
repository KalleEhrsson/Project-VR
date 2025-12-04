using UnityEngine;
using UnityEngine.InputSystem;

public class HandDriver : MonoBehaviour
{
    public InputActionProperty positionAction;
    public InputActionProperty rotationAction;

    void Update()
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
}