using UnityEngine;
using UnityEngine.InputSystem;

public class LocomotionInputBlocker : MonoBehaviour
{
    public InputActionProperty moveAction;
    public InputActionProperty turnAction;
    public InputActionProperty snapTurnAction;

    public void DisableLocomotion()
    {
        if (moveAction.action != null) moveAction.action.Disable();
        if (turnAction.action != null) turnAction.action.Disable();
        if (snapTurnAction.action != null) snapTurnAction.action.Disable();
    }

    public void EnableLocomotion()
    {
        if (moveAction.action != null) moveAction.action.Enable();
        if (turnAction.action != null) turnAction.action.Enable();
        if (snapTurnAction.action != null) snapTurnAction.action.Enable();
    }
}