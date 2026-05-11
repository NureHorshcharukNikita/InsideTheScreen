using System.Collections.Generic;

public static class DefeatedEncounters
{
    private static readonly HashSet<string> defeatedIds = new();

    public static void MarkDefeated(string encounterId)
    {
        if (!string.IsNullOrEmpty(encounterId))
            defeatedIds.Add(encounterId);
    }

    public static bool IsDefeated(string encounterId)
    {
        return !string.IsNullOrEmpty(encounterId) && defeatedIds.Contains(encounterId);
    }

    public static void Clear()
    {
        defeatedIds.Clear();
    }
}
