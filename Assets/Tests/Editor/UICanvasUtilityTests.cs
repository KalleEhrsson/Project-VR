using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class UICanvasUtilityTests
{
    #region State

    private GameObject canvasObject;
    private GameObject cameraObject;

    #endregion

    #region Setup

    [SetUp]
    public void SetUp()
    {
        canvasObject = new GameObject("TestCanvas");
        cameraObject = new GameObject("TestCamera");
    }

    [TearDown]
    public void TearDown()
    {
        if (canvasObject != null)
            Object.DestroyImmediate(canvasObject);

        if (cameraObject != null)
            Object.DestroyImmediate(cameraObject);
    }

    #endregion

    #region Tests

    [Test]
    public void ConfigureWorldSpaceCanvas_AssignsRenderModeCameraAndDefaults()
    {
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        cameraObject.AddComponent<Camera>();

        UICanvasUtility.ResetToDefaults(canvas.transform, cameraObject.GetComponent<Camera>());
        UICanvasUtility.ConfigureWorldSpaceCanvas(canvas, cameraObject.GetComponent<Camera>());

        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
        Assert.That(canvas.worldCamera, Is.EqualTo(cameraObject.GetComponent<Camera>()));
        Assert.That(canvas.transform.localScale, Is.EqualTo(Vector3.one * UICanvasUtility.DefaultScale));
    }

    [Test]
    public void SaveAndTryLoad_ReturnsSavedData()
    {
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        Camera camera = cameraObject.AddComponent<Camera>();

        UICanvasUtility.SavedCanvasData data = new UICanvasUtility.SavedCanvasData
        {
            position = new Vector3(1f, 2f, 3f),
            rotation = Quaternion.Euler(0f, 45f, 0f),
            scale = Vector3.one * 0.005f,
            distance = 1.1f,
            verticalOffset = -0.2f
        };

        UICanvasUtility.Save(canvas.transform, data);

        bool loaded = UICanvasUtility.TryLoad(canvas.transform, out UICanvasUtility.SavedCanvasData loadedData);

        Assert.That(loaded, Is.True);
        Assert.That(loadedData.position, Is.EqualTo(data.position));
        Assert.That(loadedData.rotation, Is.EqualTo(data.rotation));
        Assert.That(loadedData.scale, Is.EqualTo(data.scale));
        Assert.That(loadedData.distance, Is.EqualTo(data.distance));
        Assert.That(loadedData.verticalOffset, Is.EqualTo(data.verticalOffset));
    }

    #endregion
}
