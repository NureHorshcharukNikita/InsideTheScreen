using System.Collections.Generic;

// Who is with each enemy on the map (EnemyEncounter): rolled at the first battle against it, then
// the same group every time after, a lost battle or a reloaded save included. Each entry is an
// index into the encounter's Possible Extra Enemies, or EnemyEncounter.SameEnemy.
public static class EncounterGroups
{
    private static readonly Dictionary<string, int[]> groups = new();

    public static bool TryGet(string encounterId, out int[] extraEnemies)
    {
        extraEnemies = null;
        return !string.IsNullOrEmpty(encounterId) && groups.TryGetValue(encounterId, out extraEnemies);
    }

    public static void Remember(string encounterId, int[] extraEnemies)
    {
        if (!string.IsNullOrEmpty(encounterId) && extraEnemies != null)
            groups[encounterId] = extraEnemies;
    }

    // A respawned enemy rolls a new group.
    public static void Forget(string encounterId)
    {
        if (!string.IsNullOrEmpty(encounterId))
            groups.Remove(encounterId);
    }

    public static void Clear()
    {
        groups.Clear();
    }

    public static EncounterGroupSaveData[] ToArray()
    {
        var result = new EncounterGroupSaveData[groups.Count];
        int index = 0;
        foreach (KeyValuePair<string, int[]> group in groups)
            result[index++] = new EncounterGroupSaveData { encounterId = group.Key, extraEnemies = group.Value };

        return result;
    }

    public static void Restore(IEnumerable<EncounterGroupSaveData> savedGroups)
    {
        groups.Clear();

        if (savedGroups == null)
            return;

        foreach (EncounterGroupSaveData group in savedGroups)
        {
            if (group != null)
                Remember(group.encounterId, group.extraEnemies);
        }
    }
}
