using UnityEngine;
using UnityEngine.InputSystem;

public class LocomotionInputBlocker : MonoBehaviour
{
    #region Inspector Stuff (Input Actions)
    [Tooltip("Optional PlayerInput to resolve actions from. Auto-found if left empty.")]
    [SerializeField]
    private PlayerInput playerInput;

    [Tooltip("Input action asset to resolve actions from. Auto-resolved from PlayerInput if left empty.")]
    [SerializeField]
    private InputActionAsset actionAsset;

    [Tooltip("Move action to disable when UI is open. Auto-resolved by name when unassigned.")]
    [SerializeField]
    private InputActionProperty moveAction; // Kept serialized because bindings differ per action map

    [Tooltip("Turn action to disable when UI is open. Auto-resolved by name when unassigned.")]
    [SerializeField]
    private InputActionProperty turnAction; // Kept serialized because bindings differ per action map

    [Tooltip("Snap turn action to disable when UI is open. Auto-resolved by name when unassigned.")]
    [SerializeField]
    private InputActionProperty snapTurnAction; // Kept serialized because bindings differ per action map

    [Header("Auto-Resolve Action Names")]
    [SerializeField]
    private string moveActionName = "Move";
    [SerializeField]
    private string turnActionName = "Turn";
    [SerializeField]
    private string snapTurnActionName = "Snap Turn";
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    private void Awake()
    {
        playerInput ??= GetComponent<PlayerInput>() ?? FindFirstObjectByType<PlayerInput>();
        actionAsset ??= playerInput?.actions;

        ResolveAction(ref moveAction, moveActionName);
        ResolveAction(ref turnAction, turnActionName);
        ResolveAction(ref snapTurnAction, snapTurnActionName);

        if (moveAction.action == null || turnAction.action == null || snapTurnAction.action == null)
        {
            Debug.LogWarning("LocomotionInputBlocker is missing one or more input actions.");
        }
    }
    #endregion

    #region Public Entry Points (Called From UI)
    public void DisableLocomotion()
    {
        moveAction.action?.Disable();
        turnAction.action?.Disable();
        snapTurnAction.action?.Disable();
    }

    public void EnableLocomotion()
    {
        moveAction.action?.Enable();
        turnAction.action?.Enable();
        snapTurnAction.action?.Enable();
    }
    #endregion

    #region Helpers
    private void ResolveAction(ref InputActionProperty actionProperty, string actionName)
    {
        if (actionProperty.action != null || actionAsset == null || string.IsNullOrWhiteSpace(actionName))
        {
            return;
        }

        InputAction resolvedAction = actionAsset.FindAction(actionName);
        if (resolvedAction != null)
        {
            actionProperty = new InputActionProperty(resolvedAction);
        }
    }
    #endregion
}
