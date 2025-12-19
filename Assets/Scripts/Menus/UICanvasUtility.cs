using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UICanvasUtility
{
    // Authoritative sizing for VR menus
    public const float CanvasMetersPerPixel = 0.0012f;
    public static readonly Vector2 defaultCanvasSize = new Vector2(100f, 50f);
    public const float DefaultDistance = 0.9f;
    public const float DefaultVerticalOffset = -0.15f;

    public static void ConfigureWorldSpaceCanvas(
        Canvas canvas,
        Camera camera,
        Vector2 size,
        float metersPerPixel,
        float distance,
        float verticalOffset)
    {
        if (canvas == null)
            return;

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        canvas.pixelPerfect = false;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size;
        canvasRect.localScale = Vector3.one * metersPerPixel;

        if (camera != null)
        {
            Vector3 forward = camera.transform.forward;
            Vector3 up = camera.transform.up;
            canvasRect.position = camera.transform.position + forward * distance + up * verticalOffset;
            canvasRect.rotation = Quaternion.LookRotation(forward, up);
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;
    }

    public static void NormalizeRectTransformScales(Transform root)
    {
        if (root == null)
            return;

        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            rect.localScale = Vector3.one;
    }

    public static void ConfigureAutoSizingText(
        TextMeshProUGUI text,
        float minSize,
        float maxSize)
    {
        if (text == null)
            return;

        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }
    
    public static void ScaleChildrenRelativeToCanvas(Canvas canvas)
    {
        if (canvas == null)
            return;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;

        foreach (RectTransform rect in canvas.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect == canvasRect)
                continue;

            // Convert current pixel size into canvas-relative size
            Vector2 normalizedSize = new Vector2(
                rect.sizeDelta.x / canvasSize.x,
                rect.sizeDelta.y / canvasSize.y
            );

            rect.anchorMin = rect.anchorMin;
            rect.anchorMax = rect.anchorMax;

            // Reapply size as percentage of canvas
            rect.sizeDelta = new Vector2(
                canvasSize.x * normalizedSize.x,
                canvasSize.y * normalizedSize.y
            );

            rect.localScale = Vector3.one;
        }
    }
}
