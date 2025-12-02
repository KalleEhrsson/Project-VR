using UnityEngine;
using UnityEngine.InputSystem;

public class VRHandDriver : MonoBehaviour
{
    public InputActionProperty positionAction;
    public InputActionProperty rotationAction;

    void Update()
    {
        if (positionAction.action != null)
            transform.localPosition = positionAction.action.ReadValue<Vector3>();

        if (rotationAction.action != null)
            transform.localRotation = rotationAction.action.ReadValue<Quaternion>();
    }
}