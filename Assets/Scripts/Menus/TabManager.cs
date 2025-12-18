using UnityEngine;

public class TabManager : MonoBehaviour
{
    #region Inspector Stuff (UI Panels)
    [SerializeField]
    private GameObject[] panels; // UI-only references stay serialized for layout flexibility
    #endregion

    #region Public Entry Points (Called From UI)
    public void OpenTab(GameObject panelToOpen)
    {
        // Close all tabs
        foreach (GameObject panel in panels)
            panel.SetActive(false);

        // Open selected
        panelToOpen.SetActive(true);
    }
    #endregion
}
