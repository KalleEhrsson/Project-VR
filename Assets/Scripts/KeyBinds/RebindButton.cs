using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class RebindButton : MonoBehaviour
{
    public string actionName;
    public int bindingIndex = 0;
    public TextMeshProUGUI label;
    public Button button;
    public KeybindManager manager;

    InputActionRebindingExtensions.RebindingOperation op;

    void Start()
    {
        UpdateLabel();
        button.onClick.AddListener(StartRebind);
    }

    void UpdateLabel()
    {
        var action = manager.actions.FindAction(actionName);
        label.text = action.bindings[bindingIndex].ToDisplayString();
    }

    void StartRebind()
    {
        var action = manager.actions.FindAction(actionName);

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
}