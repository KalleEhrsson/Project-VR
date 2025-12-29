using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UICanvasUtility
{
    #region Data

    [Serializable]
    public struct SavedCanvasData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float distance;
        public float verticalOffset;
    }

    [Serializable]
    private class SavedCanvasEntry
    {
        public string key;
        public SavedCanvasData data;
    }

    [Serializable]
    private class SavedCanvasCollection
    {
        public List<SavedCanvasEntry> entries = new List<SavedCanvasEntry>();
    }

    #endregion

    #region Defaults

    public const float DefaultDistance = 0.9f;
    public const float DefaultVerticalOffset = -0.15f;
    public const float DefaultScale = 0.0012f;

    #endregion

    #region State

    private const string SaveFileName = "vr_canvas_calibration.json";
    private static bool isLoaded;
    private static SavedCanvasCollection cachedCollection;

    #endregion

    #region Public API

    public static void ConfigureWorldSpaceCanvas(Canvas canvas, Camera camera)
    {
        if (canvas == null)
            return;

        Camera targetCamera = camera != null ? camera : Camera.main;

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = targetCamera;
        canvas.pixelPerfect = false;

        EnsureCanvasScaler(canvas);
        ApplySavedOrDefault(canvas.transform, targetCamera);
    }

    public static void ApplySavedOrDefault(Transform canvasRoot, Camera camera)
    {
        if (canvasRoot == null)
            return;

        Camera targetCamera = camera != null ? camera : Camera.main;

        if (TryLoad(canvasRoot, out SavedCanvasData savedData))
        {
            ApplyData(canvasRoot, savedData);
            return;
        }

        if (targetCamera == null)
        {
            canvasRoot.localScale = Vector3.one * DefaultScale;
            return;
        }

        Vector3 forward = targetCamera.transform.forward;
        Vector3 up = targetCamera.transform.up;
        Vector3 position = targetCamera.transform.position + forward * DefaultDistance + up * DefaultVerticalOffset;

        SavedCanvasData defaultData = new SavedCanvasData
        {
            position = position,
            rotation = Quaternion.LookRotation(forward, up),
            scale = Vector3.one * DefaultScale,
            distance = DefaultDistance,
            verticalOffset = DefaultVerticalOffset
        };

        ApplyData(canvasRoot, defaultData);
    }

    public static string GetCanvasKey(Transform canvasRoot)
    {
        if (canvasRoot == null)
            return string.Empty;

        string sceneName = canvasRoot.gameObject.scene.name;
        string path = GetHierarchyPath(canvasRoot);

        return $"{sceneName}:{path}";
    }

    public static bool TryLoad(Transform canvasRoot, out SavedCanvasData data)
    {
        data = default;

        if (canvasRoot == null)
            return false;

        LoadIfNeeded();

        string key = GetCanvasKey(canvasRoot);
        foreach (SavedCanvasEntry entry in cachedCollection.entries)
        {
            if (entry.key == key)
            {
                data = entry.data;
                return true;
            }
        }

        return false;
    }

    public static void Save(Transform canvasRoot, SavedCanvasData data)
    {
        if (canvasRoot == null)
            return;

        LoadIfNeeded();

        string key = GetCanvasKey(canvasRoot);
        foreach (SavedCanvasEntry entry in cachedCollection.entries)
        {
            if (entry.key == key)
            {
                entry.data = data;
                WriteCollection();
                return;
            }
        }

        cachedCollection.entries.Add(new SavedCanvasEntry
        {
            key = key,
            data = data
        });

        WriteCollection();
    }

    public static void ResetToDefaults(Transform canvasRoot, Camera camera)
    {
        if (canvasRoot == null)
            return;

        RemoveEntry(canvasRoot);
        ApplySavedOrDefault(canvasRoot, camera);
    }

    public static void ConfigureAutoSizingText(TextMeshProUGUI text, float minSize, float maxSize)
    {
        if (text == null)
            return;

        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    #endregion

    #region Internal Helpers

    private static void ApplyData(Transform root, SavedCanvasData data)
    {
        root.position = data.position;
        root.rotation = data.rotation;
        root.localScale = data.scale;
    }

    private static void EnsureCanvasScaler(Canvas canvas)
    {
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;
    }

    private static string GetHierarchyPath(Transform root)
    {
        List<string> names = new List<string>();
        Transform current = root;

        while (current != null)
        {
            names.Add(current.name);
            current = current.parent;
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private static void LoadIfNeeded()
    {
        if (isLoaded)
            return;

        cachedCollection = new SavedCanvasCollection();

        string path = GetSavePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            if (!string.IsNullOrEmpty(json))
            {
                SavedCanvasCollection loaded = JsonUtility.FromJson<SavedCanvasCollection>(json);
                if (loaded != null)
                    cachedCollection = loaded;
            }
        }

        if (cachedCollection.entries == null)
            cachedCollection.entries = new List<SavedCanvasEntry>();

        isLoaded = true;
    }

    private static void WriteCollection()
    {
        string path = GetSavePath();
        string json = JsonUtility.ToJson(cachedCollection, true);
        File.WriteAllText(path, json);
    }

    private static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }

    private static void RemoveEntry(Transform canvasRoot)
    {
        LoadIfNeeded();

        string key = GetCanvasKey(canvasRoot);
        for (int i = cachedCollection.entries.Count - 1; i >= 0; i--)
        {
            if (cachedCollection.entries[i].key == key)
            {
                cachedCollection.entries.RemoveAt(i);
                WriteCollection();
                return;
            }
        }
    }

    #endregion
}
