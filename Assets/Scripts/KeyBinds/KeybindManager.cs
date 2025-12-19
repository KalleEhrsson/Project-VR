using UnityEngine;
using UnityEngine.InputSystem;

public class KeybindManager : MonoBehaviour
{
    #region Inspector Stuff (Input Actions)
    [Tooltip("Input actions to save and load bindings for. Auto-resolved from PlayerInput if left empty.")]
    [SerializeField]
    private InputActionAsset actions; // Falls back to PlayerInput actions if not assigned
    #endregion

    #region Public Entry Points (Called From UI)
    // Save a single binding override
    public void SaveBinding(string actionName, int bindingIndex)
    {
        var action = actions.FindAction(actionName);
        PlayerPrefs.SetString(actionName + bindingIndex, action.bindings[bindingIndex].overridePath);
    }

    // Load all overrides for one action
    public void LoadBinding(string actionName)
    {
        var action = actions.FindAction(actionName);
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
}
