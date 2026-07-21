using System;
using System.IO;
using UnityEngine;

public static class PersistentSessionSave
{
    private const string FileName = "session-save.json";

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool TryLoad(out SessionSaveData data)
    {
        data = null;

        if (!File.Exists(SavePath))
            return false;

        try
        {
            string json = File.ReadAllText(SavePath);
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
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to save session: {exception.Message}");
        }
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to delete session save: {exception.Message}");
        }
    }
}
