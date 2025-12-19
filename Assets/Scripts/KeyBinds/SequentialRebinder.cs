using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class SequentialRebinder : MonoBehaviour
{
    #region Types And Structs (What This System Uses)

    [System.Serializable]
    public struct RebindStep
    {
        public InputActionReference action;
        public string displayName;
    }

    public enum RebindState
    {
        Idle,
        WaitingForInput,
        PausedOrExited,
        Completed
    }

    #endregion

    #region Inspector Stuff (UI And Assets)

    [Tooltip("Input actions to rebind. Auto-resolved from PlayerInput if left empty.")]
    [SerializeField]
    private InputActionAsset inputActions; // Falls back to PlayerInput actions when available

    private TextMeshProUGUI instructionText;
    private TextMeshProUGUI conflictWarningText;

    [Tooltip("UI event fired when instruction text changes.")]
    [SerializeField]
    private UnityEvent<string> onInstructionChanged;

    [Tooltip("UI event fired when a conflict is detected.")]
    [SerializeField]
    private UnityEvent<string> onConflictDetected;
    
    private const float vrCanvasScale = 0.01f;

    #endregion

    #region Rebind Rules (What Is Allowed)

    private readonly HashSet<string> allowedControlTokens = new HashSet<string>
    {
        "trigger",
        "grip",
        "primarybutton",
        "secondarybutton",
        "joystick"
    };

    #endregion

    private List<RebindStep> orderedActions = new List<RebindStep>();

    #region Current State (What Is Happening Right Now)

    private const string playerPrefsKey = "VRSequentialRebinder_Bindings";

    private readonly Dictionary<string, string> boundPathsByAction = new Dictionary<string, string>();

    private int currentStepIndex = -1;
    private RebindState state = RebindState.Idle;

    private InputActionRebindingExtensions.RebindingOperation activeRebindingOperation;
    private InputAction pauseOrExitAction;
    private InputActionAsset cachedAsset;

    private string currentInstruction = string.Empty;
    private string currentConflictWarning = string.Empty;

    public string currentInstructionText => currentInstruction;
    public string currentConflictWarningText => currentConflictWarning;
    public RebindState CurrentState => state;

    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)

    private void Awake()
    {
        inputActions ??= GetComponent<PlayerInput>()?.actions;
        InitializePauseAction();
        BuildDefaultRebindSequence();
        LoadSavedBindings();

        CreateRebindUI();
    }

    private void OnEnable()
    {
        pauseOrExitAction?.Enable();
    }

    private void OnDisable()
    {
        pauseOrExitAction?.Disable();
        CancelRebindOperation();
        SaveBindings();
    }

    private void OnDestroy()
    {
        CancelRebindOperation();

        if (pauseOrExitAction != null)
            pauseOrExitAction.Dispose();
    }

    #endregion

    public void StartRebindSequence()
    {
        instructionText.gameObject.SetActive(true);
        conflictWarningText.gameObject.SetActive(true);
        
        if (orderedActions == null || orderedActions.Count == 0)
        {
            UpdateInstruction("No actions configured for rebinding.");
            return;
        }

        boundPathsByAction.Clear();
        currentStepIndex = 0;
        state = RebindState.WaitingForInput;
        BeginRebindForCurrentStep();
    }

    private void InitializePauseAction()
    {
        pauseOrExitAction = new InputAction(
            "PauseOrExit",
            InputActionType.Button,
            "<XRController>{LeftHand}/menuButton");

        pauseOrExitAction.performed += OnPauseOrExitPerformed;
    }

    private void OnPauseOrExitPerformed(InputAction.CallbackContext context)
    {
        if (state == RebindState.Idle || state == RebindState.Completed)
            return;

        CancelRebindOperation();
        SaveBindings();
        state = RebindState.PausedOrExited;
        
        instructionText.gameObject.SetActive(false);
        conflictWarningText.gameObject.SetActive(false);
        
        UpdateInstruction("Rebinding canceled. Returning to menu.");
    }

    #region Rebind Flow (The Step By Step Wizard)

    private void BeginRebindForCurrentStep()
    {
        CancelRebindOperation();

        if (state == RebindState.PausedOrExited)
            return;

        if (currentStepIndex < 0 || currentStepIndex >= orderedActions.Count)
        {
            CompleteSequence();
            return;
        }

        var entry = orderedActions[currentStepIndex];
        var action = entry.action != null ? entry.action.action : null;

        if (action == null)
        {
            AdvanceToNextStep();
            return;
        }

        int bindingIndex = GetPrimaryBindingIndex(action);

        if (bindingIndex < 0)
        {
            AdvanceToNextStep();
            return;
        }

        UpdateInstruction($"Press button for {entry.displayName}");

        bool wasEnabled = action.enabled;
        if (!wasEnabled)
            action.Enable();

        activeRebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("*/position")
            .WithControlsExcluding("*/rotation")
            .WithControlsExcluding("*/devicePose")
            .WithControlsExcluding("*/pointerPosition")
            .WithControlsExcluding("*/pointerRotation")
            .WithControlsExcluding("*/isTracked")
            .WithControlsExcluding("*/trackingState")
            .WithControlsExcluding("*/menuButton")
            .WithControlsExcluding("*/touchpad")
            .OnCancel(_ => OnRebindCanceled(action, wasEnabled))
            .OnComplete(op => OnRebindComplete(op, entry, bindingIndex, wasEnabled));

        activeRebindingOperation.Start();
        state = RebindState.WaitingForInput;
    }

    private void AdvanceToNextStep()
    {
        currentStepIndex++;

        if (currentStepIndex >= orderedActions.Count)
        {
            CompleteSequence();
            return;
        }

        BeginRebindForCurrentStep();
    }

    private void CompleteSequence()
    {
        CancelRebindOperation();
        SaveBindings();
        state = RebindState.Completed;
        UpdateInstruction("Rebinding complete.");
        
        instructionText.gameObject.SetActive(false);
        conflictWarningText.gameObject.SetActive(false);
    }

    #endregion

    #region Rebind Callbacks (When Input Is Pressed Or Canceled)

    private void OnRebindComplete(
        InputActionRebindingExtensions.RebindingOperation operation,
        RebindStep entry,
        int bindingIndex,
        bool wasEnabled)
    {
        var action = entry.action.action;
        string effectivePath = action.bindings[bindingIndex].effectivePath;

        operation.Dispose();
        activeRebindingOperation = null;

        if (!wasEnabled)
            action.Disable();

        if (!IsAllowedControlPath(effectivePath))
        {
            UpdateInstruction($"Input not allowed. Press button for {entry.displayName}");
            BeginRebindForCurrentStep();
            return;
        }

        RegisterBinding(entry.displayName, effectivePath);
        SaveBindings();
        AdvanceToNextStep();
    }

    private void OnRebindCanceled(InputAction action, bool wasEnabled)
    {
        if (!wasEnabled && action.enabled)
            action.Disable();

        activeRebindingOperation = null;
    }

    #endregion

    #region Conflict Detection (Two Things On Same Button)

    private void RegisterBinding(string displayName, string effectivePath)
    {
        if (string.IsNullOrEmpty(effectivePath))
            return;

        boundPathsByAction[displayName] = effectivePath;
        CheckForConflicts(displayName, effectivePath);
    }

    private void CheckForConflicts(string currentActionName, string currentPath)
    {
        foreach (var pair in boundPathsByAction)
        {
            if (pair.Key == currentActionName)
                continue;

            if (pair.Value == currentPath)
            {
                currentConflictWarning =
                    $"Warning: {pair.Key} and {currentActionName} are bound to the same button";

                UpdateConflictWarning(currentConflictWarning);
                return;
            }
        }

        currentConflictWarning = string.Empty;
        UpdateConflictWarning(currentConflictWarning);
    }

    #endregion

    #region Validation (Is This Input Even Allowed)

    private bool IsAllowedControlPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        string lowered = path.ToLowerInvariant();

        if (lowered.Contains("menubutton"))
            return false;

        foreach (var token in allowedControlTokens)
        {
            if (lowered.Contains(token))
                return true;
        }

        return false;
    }

    #endregion

    #region Saving And Loading (PlayerPrefs)

    private void LoadSavedBindings()
    {
        var asset = GetActionAsset();
        if (asset == null)
            return;

        cachedAsset = asset;

        if (PlayerPrefs.HasKey(playerPrefsKey))
            asset.LoadBindingOverridesFromJson(PlayerPrefs.GetString(playerPrefsKey));
    }

    private void SaveBindings()
    {
        var asset = GetActionAsset();
        if (asset == null)
            return;

        string json = asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(playerPrefsKey, json);
        PlayerPrefs.Save();
    }

    private InputActionAsset GetActionAsset()
    {
        if (cachedAsset != null)
            return cachedAsset;

        foreach (var entry in orderedActions)
        {
            if (entry.action != null &&
                entry.action.action != null &&
                entry.action.action.actionMap != null)
                return entry.action.action.actionMap.asset;
        }

        return null;
    }

    #endregion

    #region Runtime UI Creation

    private void CreateRebindUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("RebindCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        NormalizeVrCanvas(canvas);
        UICanvasUtility.NormalizeRectTransformScales(canvas.transform);

        instructionText = CreateText(
            canvas.transform,
            "RebindInstructionText",
            new Vector2(0.5f, 0.6f),
            "Press a button",
            36
        );

        conflictWarningText = CreateText(
            canvas.transform,
            "RebindConflictText",
            new Vector2(0.5f, 0.5f),
            "",
            24
        );

        conflictWarningText.color = Color.red;

        instructionText.gameObject.SetActive(false);
        conflictWarningText.gameObject.SetActive(false);
    }


    private TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        Vector2 anchor,
        string initialText,
        float maxFontSize)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        RectTransform rect = tmp.rectTransform;

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600, 120);

        tmp.text = initialText;
        tmp.alignment = TextAlignmentOptions.Center;
        UICanvasUtility.ConfigureAutoSizingText(tmp, 18f, maxFontSize);
        
        tmp.color = Color.black;

        return tmp;
    }

    #endregion
    
    #region Helpers (Small Utility Functions)

    private int GetPrimaryBindingIndex(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];
            if (!binding.isComposite && !binding.isPartOfComposite)
                return i;
        }

        return -1;
    }

    private void CancelRebindOperation()
    {
        if (activeRebindingOperation == null)
            return;

        activeRebindingOperation.Cancel();
        activeRebindingOperation.Dispose();
        activeRebindingOperation = null;
    }

    private void UpdateInstruction(string message)
    {
        currentInstruction = message;

        if (instructionText != null)
            instructionText.text = currentInstruction;

        onInstructionChanged?.Invoke(currentInstruction);
    }

    private void UpdateConflictWarning(string message)
    {
        if (conflictWarningText != null)
            conflictWarningText.text = message;

        onConflictDetected?.Invoke(message);
    }
    
    private void NormalizeVrCanvas(Canvas canvas)
    {
        Camera camera = Camera.main;

        // Single source of truth for VR menu sizing/positioning to prevent scale drift.
        UICanvasUtility.ConfigureWorldSpaceCanvas(
            canvas,
            camera,
            UICanvasUtility.defaultCanvasSize,
            UICanvasUtility.CanvasMetersPerPixel,
            UICanvasUtility.DefaultDistance,
            UICanvasUtility.DefaultVerticalOffset
        );

        UICanvasUtility.ScaleChildrenRelativeToCanvas(canvas);
    }

    private void NormalizeUiHierarchy(Transform root)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            rect.localScale = Vector3.one;
        }
    }

    #endregion

    #region Auto Setup (No Inspector Clicking)

    private void BuildDefaultRebindSequence()
    {
        orderedActions.Clear();

        if (inputActions == null)
            return;

        AddAction("Grab", "Grab");
        AddAction("Shoot", "Shoot");
        AddAction("Move", "Move");
        AddAction("Turn", "Turn");
        AddAction("Reload", "Reload");
    }

    private void AddAction(string actionName, string displayName)
    {
        var action = inputActions.FindAction(actionName, throwIfNotFound: false);
        if (action == null)
        {
            Debug.LogWarning($"Rebind action not found: {actionName}");
            return;
        }

        orderedActions.Add(new RebindStep
        {
            action = InputActionReference.Create(action),
            displayName = displayName
        });
    }

    #endregion
}
