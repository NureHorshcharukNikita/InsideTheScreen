using System;
using System.IO;
using UnityEngine;

public static class PersistentSessionSave
{
    private const string FileName = "session-save.json";
    private const string SlotFileNameFormat = "session-save-slot-{0}.json";
    private const string ActiveSlotPrefsKey = "InsideTheScreen.ActiveSaveSlot";

    public const int SlotCount = 3;

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    private static string SlotSavePath(int slotIndex) => Path.Combine(
        Application.persistentDataPath,
        string.Format(SlotFileNameFormat, Mathf.Clamp(slotIndex, 0, SlotCount - 1) + 1));

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
