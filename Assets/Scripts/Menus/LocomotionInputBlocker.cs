using UnityEngine;
using UnityEngine.InputSystem;

public class LocomotionInputBlocker : MonoBehaviour
{
    #region Inspector Stuff (Input Actions)
    [SerializeField]
    private InputActionProperty moveAction; // Kept serialized because bindings differ per action map

    [SerializeField]
    private InputActionProperty turnAction; // Kept serialized because bindings differ per action map

    [SerializeField]
    private InputActionProperty snapTurnAction; // Kept serialized because bindings differ per action map
    #endregion

    #region Public Entry Points (Called From UI)
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
    #endregion
}
