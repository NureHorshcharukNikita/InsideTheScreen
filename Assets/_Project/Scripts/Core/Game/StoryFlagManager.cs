using System;
using System.Collections.Generic;

public static class StoryFlagManager
{
    private static readonly List<string> Flags = new();

    public static event Action Changed;

    public static bool HasFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
            return false;

        for (int i = 0; i < Flags.Count; i++)
        {
            if (Flags[i] == flag)
                return true;
        }

        return false;
    }

    public static bool SetFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag) || HasFlag(flag))
            return false;

        Flags.Add(flag);
        ExplorationPlayerSession.SaveStoryFlags();
        Changed?.Invoke();
        return true;
    }

    public static bool ClearFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
            return false;

        for (int i = 0; i < Flags.Count; i++)
        {
            if (Flags[i] != flag)
                continue;

            Flags.RemoveAt(i);
            ExplorationPlayerSession.SaveStoryFlags();
            Changed?.Invoke();
            return true;
        }

        return false;
    }

    public static string[] ToArray()
    {
        string[] result = new string[Flags.Count];
        Flags.CopyTo(result);
        return result;
    }

    public static void Restore(string[] flags)
    {
        Flags.Clear();
        if (flags == null)
            return;

        for (int i = 0; i < flags.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(flags[i]) && !HasFlag(flags[i]))
                Flags.Add(flags[i]);
        }

        Changed?.Invoke();
    }

    public static void Clear()
    {
        Flags.Clear();
        Changed?.Invoke();
    }
}
