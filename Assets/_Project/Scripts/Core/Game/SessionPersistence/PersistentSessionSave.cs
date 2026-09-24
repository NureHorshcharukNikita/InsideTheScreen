using System;
using System.IO;
using UnityEngine;

public static class PersistentSessionSave
{
    private const string FileName = "session-save.json";
    private const string SlotFileNameFormat = "session-save-slot-{0}.json";
    private const string SlotPreviewFileNameFormat = "session-preview-slot-{0}.jpg";
    private const string ActiveSlotPrefsKey = "InsideTheScreen.ActiveSaveSlot";
    private const int PreviewWidth = 320;
    private const int PreviewHeight = 180;
    private const int PreviewJpegQuality = 82;

    public const int SlotCount = 3;

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    private static string SlotSavePath(int slotIndex) => Path.Combine(
        Application.persistentDataPath,
        string.Format(SlotFileNameFormat, Mathf.Clamp(slotIndex, 0, SlotCount - 1) + 1));
    private static string SlotPreviewPath(int slotIndex) => Path.Combine(
        Application.persistentDataPath,
        string.Format(SlotPreviewFileNameFormat, Mathf.Clamp(slotIndex, 0, SlotCount - 1) + 1));

    public static int ActiveSlotIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(ActiveSlotPrefsKey, 0), 0, SlotCount - 1);
        private set
        {
            PlayerPrefs.SetInt(ActiveSlotPrefsKey, Mathf.Clamp(value, 0, SlotCount - 1));
            PlayerPrefs.Save();
        }
    }

    public static void SetActiveSlot(int slotIndex)
    {
        ActiveSlotIndex = slotIndex;
    }

    public static bool HasAnySlot()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (HasSlot(i))
                return true;
        }

        return false;
    }

    public static bool HasSlot(int slotIndex)
    {
        return TryLoadSlot(slotIndex, out _);
    }

    public static bool TryGetSlotInfo(int slotIndex, out SessionSaveData data)
    {
        return TryLoadSlot(slotIndex, out data);
    }

    public static bool TryLoad(out SessionSaveData data)
    {
        return TryLoadSlot(ActiveSlotIndex, out data);
    }

    public static bool TryLoadSlot(int slotIndex, out SessionSaveData data)
    {
        data = null;
        string savePath = GetReadableSavePath(slotIndex);

        if (!File.Exists(savePath))
            return false;

        try
        {
            string json = File.ReadAllText(savePath);
            data = JsonUtility.FromJson<SessionSaveData>(json);
            return data != null && data.hasSession;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load session save: {exception.Message}");
            data = null;
            return false;
        }
    }

    public static void Save(SessionSaveData data)
    {
        if (data == null || !data.hasSession)
            return;

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            data.savedAtTicks = DateTime.Now.Ticks;
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SlotSavePath(ActiveSlotIndex), json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to save session: {exception.Message}");
        }
    }

    public static void CaptureActiveSlotPreview()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        RenderTexture previewTarget = RenderTexture.GetTemporary(
            PreviewWidth,
            PreviewHeight,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        bool[] canvasStates = new bool[canvases.Length];
        Texture2D preview = null;

        try
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvasStates[i] = canvases[i].enabled;
                canvases[i].enabled = false;
            }

            camera.targetTexture = previewTarget;
            camera.Render();
            RenderTexture.active = previewTarget;

            preview = new Texture2D(PreviewWidth, PreviewHeight, TextureFormat.RGB24, false);
            preview.ReadPixels(new Rect(0f, 0f, PreviewWidth, PreviewHeight), 0, 0, false);
            preview.Apply(false, false);

            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllBytes(
                SlotPreviewPath(ActiveSlotIndex),
                preview.EncodeToJPG(PreviewJpegQuality));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to capture save preview: {exception.Message}");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null)
                    canvases[i].enabled = canvasStates[i];
            }

            RenderTexture.ReleaseTemporary(previewTarget);
            if (preview != null)
                UnityEngine.Object.Destroy(preview);
        }
    }

    public static bool TryLoadPreview(int slotIndex, out Texture2D preview)
    {
        preview = null;
        string previewPath = SlotPreviewPath(slotIndex);
        if (!File.Exists(previewPath))
            return false;

        try
        {
            byte[] imageData = File.ReadAllBytes(previewPath);
            preview = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (ImageConversion.LoadImage(preview, imageData, false))
                return true;

            UnityEngine.Object.Destroy(preview);
            preview = null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to load save preview: {exception.Message}");
            if (preview != null)
                UnityEngine.Object.Destroy(preview);
            preview = null;
        }

        return false;
    }

    public static void Delete()
    {
        DeleteSlot(ActiveSlotIndex);
    }

    public static void DeleteSlot(int slotIndex)
    {
        try
        {
            string slotPath = SlotSavePath(slotIndex);
            if (File.Exists(slotPath))
                File.Delete(slotPath);

            string previewPath = SlotPreviewPath(slotIndex);
            if (File.Exists(previewPath))
                File.Delete(previewPath);

            if (slotIndex == 0 && File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to delete session save: {exception.Message}");
        }
    }

    private static string GetReadableSavePath(int slotIndex)
    {
        string slotPath = SlotSavePath(slotIndex);
        if (File.Exists(slotPath))
            return slotPath;

        return slotIndex == 0 ? SavePath : slotPath;
    }
}
