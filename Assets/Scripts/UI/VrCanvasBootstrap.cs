using UnityEngine;
using UnityEngine.SceneManagement;

public class VrCanvasBootstrap : MonoBehaviour
{
    #region Unity Lifecycle

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyToWorldSpaceCanvases();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region Scene Handling

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToWorldSpaceCanvases();
    }

    #endregion

    #region Canvas Application

    private void ApplyToWorldSpaceCanvases()
    {
        Camera camera = Camera.main;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
                continue;

            UICanvasUtility.ConfigureWorldSpaceCanvas(canvas, camera);
        }
    }

    #endregion
}
