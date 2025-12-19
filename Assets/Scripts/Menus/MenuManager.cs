using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    #region Inspector Stuff (UI Panels And Helpers)
    [Header("UI Panels")]
    [Tooltip("Root panel for the main menu. Must be assigned because layout differs per scene.")]
    [SerializeField]
    private GameObject mainPanel;

    [Tooltip("Root panel for settings. Must be assigned because layout differs per scene.")]
    [SerializeField]
    private GameObject settingsPanel;

    [Tooltip("Back button object that is shown when leaving the main panel.")]
    [SerializeField]
    private GameObject backButton;

    [Tooltip("Sub-panels within settings. Manual assignment preserves menu layout flexibility.")]
    [SerializeField]
    private GameObject[] subPanels;

    [Header("Optional Helpers")]
    [Tooltip("Tab manager used for sub-panel switching. Auto-resolved if left empty.")]
    [SerializeField]
    private TabManager tabManager; // UI-only reference kept serialized for menu wiring

    [Tooltip("Rebind manager used for control rebinding. Auto-resolved if left empty.")]
    [SerializeField]
    private SequentialRebinder rebindManager;
    #endregion

    #region Cached Components (Self Setup)
    private LocomotionInputBlocker locomotionBlocker;
    #endregion

    #region Unity Lifetime (Awake Start)
    private void Awake()
    {
        // Auto-find the locomotion blocker in the scene
        locomotionBlocker = FindFirstObjectByType<LocomotionInputBlocker>();
        tabManager ??= GetComponentInChildren<TabManager>(true);
        rebindManager ??= GetComponentInChildren<SequentialRebinder>(true);

        ValidateReferences();

        if (locomotionBlocker == null)
            Debug.LogWarning("LocomotionInputBlocker was not found in the scene.");
    }
    
    private void Start()
    {
        if (!ArePanelsValid())
            return;

        if (locomotionBlocker != null)
            locomotionBlocker.DisableLocomotion();

        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);

        foreach (GameObject panel in subPanels)
            panel.SetActive(false);

        UpdateBackButton();
    }
    #endregion

    #region Public Entry Points (Called From UI)
    public void StartGame()
    {
        if (locomotionBlocker != null)
            locomotionBlocker.EnableLocomotion();
        SceneManager.LoadScene("Game");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenSettings()
    {
        // Show settings root
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);

        // Close all sub-panels
        foreach (GameObject panel in subPanels)
            panel.SetActive(false);

        // Do NOT open any default tab
        UpdateBackButton();
    }

    public void CloseButton()
    {
        // Close sub-panel first
        foreach (GameObject panel in subPanels)
        {
            if (panel.activeSelf)
            {
                panel.SetActive(false);
                UpdateBackButton();
                return;
            }
        }

        // No sub-panel → go back to main menu
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);

        UpdateBackButton();
    }
    
    public void BeginControlRebind()
    {
        if (rebindManager != null)
            rebindManager.StartRebindSequence();
        else
            Debug.LogWarning("VRSequentialRebinder reference is not assigned on MenuManager.");
    }
    #endregion

    #region Helpers (Small Utility Functions)
    private void UpdateBackButton()
    {
        // Main menu → no back button
        if (mainPanel.activeSelf)
        {
            backButton.SetActive(false);
            return;
        }

        // Settings or sub-panel → show it
        backButton.SetActive(true);
    }
    #endregion

    #region Validation (Runtime Safety)
    private void ValidateReferences()
    {
        if (mainPanel == null)
            Debug.LogError("MenuManager requires Main Panel assignment.");
        if (settingsPanel == null)
            Debug.LogError("MenuManager requires Settings Panel assignment.");
        if (backButton == null)
            Debug.LogError("MenuManager requires Back Button assignment.");
        if (subPanels == null)
            Debug.LogWarning("MenuManager has no sub-panels assigned.");
    }

    private bool ArePanelsValid()
    {
        if (mainPanel == null || settingsPanel == null || backButton == null)
        {
            Debug.LogError("MenuManager missing required panel references. Disabling.");
            enabled = false;
            return false;
        }

        if (subPanels == null)
            subPanels = new GameObject[0];

        return true;
    }
    #endregion
}
