using UnityEngine;

public class TabManager : MonoBehaviour
{
    #region Inspector Stuff (UI Panels)
    [Tooltip("Panels managed by this tab group. Manual assignment preserves layout order.")]
    [SerializeField]
    private GameObject[] panels; // UI-only references stay serialized for layout flexibility
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    private void Awake()
    {
        if (panels == null || panels.Length == 0)
            Debug.LogWarning($"TabManager on {name} has no panels assigned.");
    }
    #endregion

    #region Public Entry Points (Called From UI)
    public void OpenTab(GameObject panelToOpen)
    {
        if (panels == null || panels.Length == 0 || panelToOpen == null)
            return;

        // Close all tabs
        foreach (GameObject panel in panels)
            panel.SetActive(false);

        // Open selected
        panelToOpen.SetActive(true);
    }
    #endregion
}
