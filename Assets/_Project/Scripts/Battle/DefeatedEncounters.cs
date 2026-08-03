using System.Collections.Generic;
using System.Linq;

public static class DefeatedEncounters
{
    private static readonly HashSet<string> defeatedIds = new();

    public static void MarkDefeated(string encounterId)
    {
        if (!string.IsNullOrEmpty(encounterId))
        {
            defeatedIds.Add(encounterId);
            ExplorationPlayerSession.SavePersistent();
        }
    }

    public static bool IsDefeated(string encounterId)
    {
        return !string.IsNullOrEmpty(encounterId) && defeatedIds.Contains(encounterId);
    }

    public static bool HasAny()
    {
        return defeatedIds.Count > 0;
    }

    public static void Clear()
    {
        defeatedIds.Clear();
        ExplorationPlayerSession.SavePersistent();
    }

    public static string[] ToArray()
    {
        return defeatedIds.ToArray();
    }

    public static void Restore(IEnumerable<string> encounterIds)
    {
        defeatedIds.Clear();

        if (encounterIds == null)
            return;

        foreach (string encounterId in encounterIds)
        {
            if (!string.IsNullOrEmpty(encounterId))
                defeatedIds.Add(encounterId);
        }
    }
}
