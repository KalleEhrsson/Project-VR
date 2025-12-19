using UnityEngine;
using UnityEngine.InputSystem;

public class LocomotionInputBlocker : MonoBehaviour
{
    #region Inspector Stuff (Input Actions)
    [Tooltip("Move action to disable when UI is open. Must be assigned per input setup.")]
    [SerializeField]
    private InputActionProperty moveAction; // Kept serialized because bindings differ per action map

    [Tooltip("Turn action to disable when UI is open. Must be assigned per input setup.")]
    [SerializeField]
    private InputActionProperty turnAction; // Kept serialized because bindings differ per action map

    [Tooltip("Snap turn action to disable when UI is open. Must be assigned per input setup.")]
    [SerializeField]
    private InputActionProperty snapTurnAction; // Kept serialized because bindings differ per action map
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    private void Awake()
    {
        if (moveAction.action == null || turnAction.action == null || snapTurnAction.action == null)
        {
            Debug.LogWarning("LocomotionInputBlocker is missing one or more input actions.");
        }
    }
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
