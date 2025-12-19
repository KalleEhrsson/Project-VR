using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    #region Inspector Stuff (UI Panels And Helpers)
    [SerializeField]
    private GameObject mainPanel;          

    [SerializeField]
    private GameObject settingsPanel;      

    [SerializeField]
    private GameObject backButton;         

    [SerializeField]
    private GameObject[] subPanels;        

    [SerializeField]
    private TabManager tabManager; // UI-only reference kept serialized for menu wiring

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

        if (locomotionBlocker == null)
            Debug.LogWarning("LocomotionInputBlocker was not found in the scene.");
    }
    
    private void Start()
    {
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
            Debug.LogWarning("SequentialRebinder reference is not assigned on MenuManager.");
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
}
