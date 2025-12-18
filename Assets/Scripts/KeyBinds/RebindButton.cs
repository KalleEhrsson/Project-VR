using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class RebindButton : MonoBehaviour
{
    #region Inspector Stuff (UI And Binding Info)
    [SerializeField]
    private string actionName;

    [SerializeField]
    private int bindingIndex = 0;

    [SerializeField]
    private TextMeshProUGUI label; // UI text for showing the current binding

    [SerializeField]
    private Button button; // UI button that triggers rebinding

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
    }

    private void Start()
    {
        UpdateLabel();
        button.onClick.AddListener(StartRebind);
    }
    #endregion

    #region Main Logic (What Actually Happens)
    private void UpdateLabel()
    {
        if (manager == null || manager.actions == null)
            return;

        var action = manager.actions.FindAction(actionName);
        if (action == null || label == null)
            return;

        label.text = action.bindings[bindingIndex].ToDisplayString();
    }

    private void StartRebind()
    {
        if (manager == null || manager.actions == null)
            return;

        var action = manager.actions.FindAction(actionName);
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
