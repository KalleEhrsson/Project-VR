using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class UICanvasUtilityTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject obj in createdObjects)
        {
            if (obj != null)
                Object.DestroyImmediate(obj);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ConfigureWorldSpaceCanvas_AssignsRenderModeScaleAndSize()
    {
        GameObject canvasObject = CreateGameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvasObject.AddComponent<CanvasScaler>();

        GameObject cameraObject = CreateGameObject("Camera");
        Camera camera = cameraObject.AddComponent<Camera>();

        Vector2 size = new Vector2(120f, 80f);
        float metersPerPixel = 0.0025f;

        UICanvasUtility.ConfigureWorldSpaceCanvas(
            canvas,
            camera,
            size,
            metersPerPixel,
            1.2f,
            -0.3f);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
        Assert.That(canvasRect.sizeDelta, Is.EqualTo(size));
        Assert.That(canvasRect.localScale, Is.EqualTo(Vector3.one * metersPerPixel));
    }

    [Test]
    public void ConfigureWorldSpaceCanvas_AddsAndConfiguresCanvasScaler()
    {
        GameObject canvasObject = CreateGameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();

        GameObject cameraObject = CreateGameObject("Camera");
        Camera camera = cameraObject.AddComponent<Camera>();

        UICanvasUtility.ConfigureWorldSpaceCanvas(
            canvas,
            camera,
            new Vector2(120f, 80f),
            0.0025f,
            1.2f,
            -0.3f);

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

        Assert.That(scaler, Is.Not.Null);
        Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ConstantPixelSize));
        Assert.That(scaler.scaleFactor, Is.EqualTo(1f));
        Assert.That(scaler.dynamicPixelsPerUnit, Is.EqualTo(10f));
        Assert.That(scaler.referencePixelsPerUnit, Is.EqualTo(100f));
    }
    
    [Test]
    public void ScaleChildrenRelativeToCanvas_NormalizesChildSizeAgainstCanvas()
    {
        GameObject canvasObject = CreateGameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200f, 100f);

        GameObject childObject = CreateGameObject("Child");
        childObject.transform.SetParent(canvasObject.transform, false);
        RectTransform childRect = childObject.AddComponent<RectTransform>();
        childRect.sizeDelta = new Vector2(40f, 10f);
        childRect.localScale = new Vector3(2f, 2f, 2f);

        Vector2 expectedNormalized = new Vector2(
            childRect.sizeDelta.x / canvasRect.sizeDelta.x,
            childRect.sizeDelta.y / canvasRect.sizeDelta.y);

        UICanvasUtility.ScaleChildrenRelativeToCanvas(canvas);

        Vector2 actualNormalized = new Vector2(
            childRect.sizeDelta.x / canvasRect.sizeDelta.x,
            childRect.sizeDelta.y / canvasRect.sizeDelta.y);

        Assert.That(actualNormalized, Is.EqualTo(expectedNormalized));
        Assert.That(childRect.localScale, Is.EqualTo(Vector3.one));
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject obj = new GameObject(name);
        createdObjects.Add(obj);
        return obj;
    }
}
