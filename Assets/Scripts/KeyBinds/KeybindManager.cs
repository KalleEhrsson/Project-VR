using UnityEngine;
using UnityEngine.InputSystem;

public class KeybindManager : MonoBehaviour
{
    #region Inspector Stuff (Input Actions)
    
    [Tooltip("Input actions to save and load bindings for. Auto-resolved from PlayerInput if left empty.")]
    [SerializeField]
    private InputActionAsset actions; // Falls back to PlayerInput actions if not assigned
    
    public InputActionAsset Actions => actions;
    
    #endregion

    #region Public Entry Points (Called From UI)
    // Save a single binding override
    public void SaveBinding(string actionName, int bindingIndex)
    {
        if (!TryGetAction(actionName, out var action))
        {
            return;
        }

        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            Debug.LogWarning($"Binding index {bindingIndex} is out of range for action {actionName}.");
            return;
        }
        
        PlayerPrefs.SetString(actionName + bindingIndex, action.bindings[bindingIndex].overridePath);
    }

    // Load all overrides for one action
    public void LoadBinding(string actionName)
    {
        if (!TryGetAction(actionName, out var action))
        {
            return;
        }
        for (int i = 0; i < action.bindings.Count; i++)
        {
            string key = actionName + i;
            if (PlayerPrefs.HasKey(key))
            {
                action.ApplyBindingOverride(i, PlayerPrefs.GetString(key));
            }
        }
    }
    
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    // Load everything on startup
    private void Awake()
    {
        actions ??= GetComponent<PlayerInput>()?.actions;
        if (actions == null)
        {
            Debug.LogWarning($"KeybindManager on {name} has no InputActionAsset assigned.");
            return;
        }

        foreach (var map in actions.actionMaps)
        {
            foreach (var action in map.actions)
                LoadBinding(action.name);
        }
    }
    #endregion
    
    #region Helpers
    private bool TryGetAction(string actionName, out InputAction action)
    {
        action = null;

        if (actions == null)
        {
            Debug.LogWarning($"KeybindManager on {name} has no InputActionAsset assigned.");
            return false;
        }

        action = actions.FindAction(actionName);
        if (action == null)
        {
            Debug.LogWarning($"Action {actionName} was not found in InputActionAsset on {name}.");
            return false;
        }

        return true;
    }
    #endregion
}
