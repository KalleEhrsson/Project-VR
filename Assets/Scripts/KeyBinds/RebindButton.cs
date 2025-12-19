using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class RebindButton : MonoBehaviour
{
    #region Inspector Stuff (UI And Binding Info)
    [Tooltip("Name of the input action to rebind.")]
    [SerializeField]
    private string actionName;

    [Tooltip("Index of the binding to rebind on the action.")]
    [SerializeField]
    private int bindingIndex = 0;

    [Tooltip("Label that displays the current binding.")]
    [SerializeField]
    private TextMeshProUGUI label; // UI text for showing the current binding

    [Tooltip("Button that triggers rebinding. Auto-resolved from this object if left empty.")]
    [SerializeField]
    private Button button; // UI button that triggers rebinding

    [Tooltip("Keybind manager used to access input actions. Auto-resolved from parent if left empty.")]
    [SerializeField]
    private KeybindManager manager; // Resolved at runtime if not assigned for convenience
    #endregion

    #region Current State (What Is Happening Right Now)
    private InputActionRebindingExtensions.RebindingOperation op;
    #endregion

    #region Unity Lifetime (Awake Start)
    private void Awake()
    {
        button ??= GetComponent<Button>();
        label ??= GetComponentInChildren<TextMeshProUGUI>();
        manager ??= GetComponentInParent<KeybindManager>();

        if (button == null)
        {
            Debug.LogError($"RebindButton on {name} requires a Button component. Disabling.");
            enabled = false;
        }

        if (label == null)
            Debug.LogWarning($"RebindButton on {name} has no label assigned.");
    }

    private void Start()
    {
        if (!enabled)
            return;

        UpdateLabel();
        button.onClick.AddListener(StartRebind);
    }
    #endregion

    #region Main Logic (What Actually Happens)
    private void UpdateLabel()
    {
        if (manager == null || manager.Actions == null)
            return;

        var action = manager.Actions.FindAction(actionName);
        if (action == null || label == null)
            return;

        label.text = action.bindings[bindingIndex].ToDisplayString();
    }

    private void StartRebind()
    {
        if (manager == null || manager.Actions == null)
            return;

        var action = manager.Actions.FindAction(actionName);
        if (action == null)
            return;

        button.interactable = false;
        label.text = "Press any key...";

        op = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(operation =>
            {
                manager.SaveBinding(actionName, bindingIndex);
                op.Dispose();
                button.interactable = true;
                UpdateLabel();
            })
            .Start();
    }
    #endregion
}
