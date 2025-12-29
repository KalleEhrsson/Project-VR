using System.Collections.Generic;
using UnityEngine;

public class VrCanvasCalibrator : MonoBehaviour
{
    #region Constants

    private const KeyCode ToggleKey = KeyCode.F9;
    private const KeyCode ResetKey = KeyCode.F10;
    private const KeyCode SaveKey = KeyCode.F5;
    private const KeyCode DistanceIncreaseKey = KeyCode.R;
    private const KeyCode DistanceDecreaseKey = KeyCode.F;
    private const KeyCode VerticalIncreaseKey = KeyCode.T;
    private const KeyCode VerticalDecreaseKey = KeyCode.G;
    private const KeyCode ScaleIncreaseKey = KeyCode.Equals;
    private const KeyCode ScaleDecreaseKey = KeyCode.Minus;

    private const float ScaleMin = 0.0005f;
    private const float ScaleMax = 0.01f;
    private const float ScaleStep = 0.0002f;
    private const float DistanceStep = 0.05f;
    private const float VerticalStep = 0.03f;

    #endregion

    #region State

    private readonly List<Transform> canvasRoots = new List<Transform>();
    private bool isCalibrating;
    private float currentScale;
    private float currentDistance;
    private float currentVerticalOffset;

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        RefreshCanvases();
        SyncFromFirstRoot();
    }

    private void Update()
    {
        HandleToggle();

        if (!isCalibrating)
            return;

        RefreshCanvases();
        HandleCalibrationInput();
    }

    private void OnGUI()
    {
        if (!isCalibrating)
            return;

        string message =
            $"VR Canvas Calibration\n" +
            $"Mode: {(isCalibrating ? "Enabled" : "Disabled")}\n" +
            $"Scale: {currentScale:F4}\n" +
            $"Distance: {currentDistance:F2}m\n" +
            $"Vertical Offset: {currentVerticalOffset:F2}m\n" +
            "Controls:\n" +
            "F9 Toggle | F10 Reset | F5 Save\n" +
            "Mouse Drag: Move | Scroll/+-: Scale\n" +
            "R/F: Distance | T/G: Vertical";

        GUI.Box(new Rect(10, 10, 280, 180), message);
    }

    #endregion

    #region Calibration Flow

    private void HandleToggle()
    {
        if (!Input.GetKeyDown(ToggleKey))
            return;

        isCalibrating = !isCalibrating;
        RefreshCanvases();

        if (isCalibrating)
        {
            SyncFromFirstRoot();
        }
        else
        {
            SaveAll();
            ApplySavedState();
        }
    }

    private void HandleCalibrationInput()
    {
        if (Input.GetKeyDown(ResetKey))
        {
            ResetAll();
            return;
        }

        if (Input.GetKeyDown(SaveKey))
            SaveAll();

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f)
            AdjustScale(scroll * ScaleStep);

        if (Input.GetKey(ScaleIncreaseKey))
            AdjustScale(ScaleStep);

        if (Input.GetKey(ScaleDecreaseKey))
            AdjustScale(-ScaleStep);

        if (Input.GetKey(DistanceIncreaseKey))
            AdjustDistance(DistanceStep);

        if (Input.GetKey(DistanceDecreaseKey))
            AdjustDistance(-DistanceStep);

        if (Input.GetKey(VerticalIncreaseKey))
            AdjustVerticalOffset(VerticalStep);

        if (Input.GetKey(VerticalDecreaseKey))
            AdjustVerticalOffset(-VerticalStep);

        if (Input.GetMouseButton(0))
            DragMove();
    }

    #endregion

    #region Canvas Discovery

    private void RefreshCanvases()
    {
        canvasRoots.Clear();

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
                continue;

            Transform root = ResolveCanvasRoot(canvas.transform);
            if (root != null && !canvasRoots.Contains(root))
                canvasRoots.Add(root);
        }
    }

    private Transform ResolveCanvasRoot(Transform canvasTransform)
    {
        Transform current = canvasTransform;
        while (current != null)
        {
            if (current.name == "VrCanvasRoot")
                return current;

            current = current.parent;
        }

        return canvasTransform;
    }

    #endregion

    #region State Sync

    private void SyncFromFirstRoot()
    {
        if (canvasRoots.Count == 0)
            return;

        Camera camera = Camera.main;
        Transform root = canvasRoots[0];

        currentScale = root.localScale.x;

        if (camera == null)
            return;

        Vector3 offset = root.position - camera.transform.position;
        currentDistance = Vector3.Dot(camera.transform.forward, offset);
        currentVerticalOffset = Vector3.Dot(camera.transform.up, offset);
    }

    private void ApplyTransformToRoots()
    {
        if (canvasRoots.Count == 0)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        Vector3 forward = camera.transform.forward;
        Vector3 up = camera.transform.up;
        Vector3 position = camera.transform.position + forward * currentDistance + up * currentVerticalOffset;
        Quaternion rotation = Quaternion.LookRotation(forward, up);
        Vector3 scale = Vector3.one * currentScale;

        foreach (Transform root in canvasRoots)
        {
            root.position = position;
            root.rotation = rotation;
            root.localScale = scale;
        }
    }

    private void ApplySavedState()
    {
        Camera camera = Camera.main;
        foreach (Transform root in canvasRoots)
            UICanvasUtility.ApplySavedOrDefault(root, camera);
    }

    #endregion

    #region Adjustments

    private void AdjustScale(float delta)
    {
        currentScale = Mathf.Clamp(currentScale + delta, ScaleMin, ScaleMax);
        ApplyTransformToRoots();
    }

    private void AdjustDistance(float delta)
    {
        currentDistance = Mathf.Max(0.05f, currentDistance + delta);
        ApplyTransformToRoots();
    }

    private void AdjustVerticalOffset(float delta)
    {
        currentVerticalOffset += delta;
        ApplyTransformToRoots();
    }

    private void DragMove()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        Plane plane = new Plane(camera.transform.forward, camera.transform.position + camera.transform.forward * currentDistance);
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (!plane.Raycast(ray, out float enter))
            return;

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector3 position = hitPoint + camera.transform.up * currentVerticalOffset;
        Quaternion rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
        Vector3 scale = Vector3.one * currentScale;

        foreach (Transform root in canvasRoots)
        {
            root.position = position;
            root.rotation = rotation;
            root.localScale = scale;
        }
    }

    #endregion

    #region Save/Reset

    private void SaveAll()
    {
        if (canvasRoots.Count == 0)
            return;

        Camera camera = Camera.main;

        foreach (Transform root in canvasRoots)
        {
            UICanvasUtility.SavedCanvasData data = BuildData(root, camera);
            UICanvasUtility.Save(root, data);
        }
    }

    private void ResetAll()
    {
        Camera camera = Camera.main;

        foreach (Transform root in canvasRoots)
            UICanvasUtility.ResetToDefaults(root, camera);

        SyncFromFirstRoot();
    }

    private UICanvasUtility.SavedCanvasData BuildData(Transform root, Camera camera)
    {
        float distance = 0f;
        float verticalOffset = 0f;

        if (camera != null)
        {
            Vector3 offset = root.position - camera.transform.position;
            distance = Vector3.Dot(camera.transform.forward, offset);
            verticalOffset = Vector3.Dot(camera.transform.up, offset);
        }

        return new UICanvasUtility.SavedCanvasData
        {
            position = root.position,
            rotation = root.rotation,
            scale = root.localScale,
            distance = distance,
            verticalOffset = verticalOffset
        };
    }

    #endregion
}
