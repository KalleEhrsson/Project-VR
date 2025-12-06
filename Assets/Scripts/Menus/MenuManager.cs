using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    private LocomotionInputBlocker locomotionBlocker;
    public GameObject mainPanel;          
    public GameObject settingsPanel;      
    public GameObject backButton;         
    public GameObject[] subPanels;        
    public TabManager tabManager;

    void Awake()
    {
        // Auto-find the locomotion blocker in the scene
        locomotionBlocker = FindFirstObjectByType<LocomotionInputBlocker>();

        if (locomotionBlocker == null)
            Debug.LogWarning("LocomotionInputBlocker was not found in the scene.");
    }
    
    void Start()
    {
        if (locomotionBlocker != null)
            locomotionBlocker.DisableLocomotion();

        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);

        foreach (GameObject panel in subPanels)
            panel.SetActive(false);

        UpdateBackButton();
    }

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

    void UpdateBackButton()
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
}