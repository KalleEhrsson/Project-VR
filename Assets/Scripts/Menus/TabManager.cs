using UnityEngine;

public class TabManager : MonoBehaviour
{
    public GameObject[] panels; // All tab panels

    public void OpenTab(GameObject panelToOpen)
    {
        // Close all tabs
        foreach (GameObject panel in panels)
            panel.SetActive(false);

        // Open selected
        panelToOpen.SetActive(true);
    }
}