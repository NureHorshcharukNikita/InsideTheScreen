using System;
using System.Collections.Generic;

public static class NpcDialogueProgress
{
    private static readonly List<NpcDialogueProgressSaveData> Npcs = new();

    public static int GetTalkCount(string npcKey)
    {
        NpcDialogueProgressSaveData progress = GetProgress(npcKey);
        return progress != null ? Math.Max(0, progress.talkCount) : 0;
    }

    public static bool WasDialogueUsed(string npcKey, string dialogueKey)
    {
        if (string.IsNullOrWhiteSpace(dialogueKey))
            return false;

        NpcDialogueProgressSaveData progress = GetProgress(npcKey);
        if (progress?.usedDialogueKeys == null)
            return false;

        for (int i = 0; i < progress.usedDialogueKeys.Length; i++)
        {
            if (progress.usedDialogueKeys[i] == dialogueKey)
                return true;
        }

        return false;
    }

    public static void RecordDialogueFinished(string npcKey, string dialogueKey)
    {
        if (string.IsNullOrWhiteSpace(npcKey))
            return;

        NpcDialogueProgressSaveData progress = GetOrCreateProgress(npcKey);
        progress.talkCount = Math.Max(0, progress.talkCount) + 1;

        if (!string.IsNullOrWhiteSpace(dialogueKey) && !WasDialogueUsed(npcKey, dialogueKey))
            progress.usedDialogueKeys = AddUsedDialogueKey(progress.usedDialogueKeys, dialogueKey);

        ExplorationPlayerSession.SaveDialogueProgress();
    }

    public static NpcDialogueProgressSaveData[] ToArray()
    {
        NpcDialogueProgressSaveData[] result = new NpcDialogueProgressSaveData[Npcs.Count];
        for (int i = 0; i < Npcs.Count; i++)
        {
            result[i] = new NpcDialogueProgressSaveData
            {
                npcKey = Npcs[i].npcKey,
                talkCount = Npcs[i].talkCount,
                usedDialogueKeys = Copy(Npcs[i].usedDialogueKeys)
            };
        }

        return result;
    }

    public static void Restore(NpcDialogueProgressSaveData[] savedNpcs)
    {
        Npcs.Clear();
        if (savedNpcs == null)
            return;

        for (int i = 0; i < savedNpcs.Length; i++)
        {
            NpcDialogueProgressSaveData savedNpc = savedNpcs[i];
            if (savedNpc == null || string.IsNullOrWhiteSpace(savedNpc.npcKey))
                continue;

            Npcs.Add(new NpcDialogueProgressSaveData
            {
                npcKey = savedNpc.npcKey,
                talkCount = Math.Max(0, savedNpc.talkCount),
                usedDialogueKeys = Copy(savedNpc.usedDialogueKeys)
            });
        }
    }

    public static void Clear()
    {
        Npcs.Clear();
    }

    private static NpcDialogueProgressSaveData GetOrCreateProgress(string npcKey)
    {
        NpcDialogueProgressSaveData progress = GetProgress(npcKey);
        if (progress != null)
            return progress;

        progress = new NpcDialogueProgressSaveData
        {
            npcKey = npcKey,
            talkCount = 0,
            usedDialogueKeys = Array.Empty<string>()
        };

        Npcs.Add(progress);
        return progress;
    }

    private static NpcDialogueProgressSaveData GetProgress(string npcKey)
    {
        if (string.IsNullOrWhiteSpace(npcKey))
            return null;

        for (int i = 0; i < Npcs.Count; i++)
        {
            if (Npcs[i] != null && Npcs[i].npcKey == npcKey)
                return Npcs[i];
        }

        return null;
    }

    private static string[] AddUsedDialogueKey(string[] keys, string dialogueKey)
    {
        int length = keys != null ? keys.Length : 0;
        string[] result = new string[length + 1];
        for (int i = 0; i < length; i++)
            result[i] = keys[i];

        result[length] = dialogueKey;
        return result;
    }

    private static string[] Copy(string[] values)
    {
        if (values == null || values.Length == 0)
            return Array.Empty<string>();

        string[] result = new string[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}
