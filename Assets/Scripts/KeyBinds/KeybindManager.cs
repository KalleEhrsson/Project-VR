using UnityEngine;
using UnityEngine.InputSystem;

public class KeybindManager : MonoBehaviour
{
    public InputActionAsset actions;

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

    // Load everything on startup
    void Awake()
    {
        foreach (var map in actions.actionMaps)
        {
            foreach (var action in map.actions)
                LoadBinding(action.name);
        }
    }
}