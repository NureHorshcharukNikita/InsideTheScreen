using System.Collections.Generic;
using System.Linq;

// The encounters the player has beaten, and after how many won battles: an enemy that respawns
// comes back once the player has won enough other battles since (EnemyEncounter).
public static class DefeatedEncounters
{
    private static readonly HashSet<string> defeatedIds = new();
    private static readonly Dictionary<string, int> defeatedAtBattle = new();

    // Battles won this game, counting every victory.
    public static int BattlesWon { get; private set; }

    public static void MarkDefeated(string encounterId)
    {
        BattlesWon++;
        if (string.IsNullOrEmpty(encounterId))
            return;

        defeatedIds.Add(encounterId);
        defeatedAtBattle[encounterId] = BattlesWon;
    }

    public static bool IsDefeated(string encounterId)
    {
        return !string.IsNullOrEmpty(encounterId) && defeatedIds.Contains(encounterId);
    }

    // How many battles the player has won since this encounter was beaten.
    public static int BattlesSince(string encounterId)
    {
        return defeatedAtBattle.TryGetValue(encounterId ?? string.Empty, out int battle)
            ? BattlesWon - battle
            : int.MaxValue;
    }

    // The encounter is back on the map, as if never beaten.
    public static void Forget(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
            return;

        defeatedIds.Remove(encounterId);
        defeatedAtBattle.Remove(encounterId);
    }

    public static bool HasAny()
    {
        return defeatedIds.Count > 0;
    }

    public static void Clear()
    {
        defeatedIds.Clear();
        defeatedAtBattle.Clear();
        BattlesWon = 0;
    }

    public static string[] ToArray()
    {
        return defeatedIds.ToArray();
    }

    // When each of ToArray's encounters was beaten, in the same order.
    public static int[] DefeatedAtToArray()
    {
        return defeatedIds.Select(id => defeatedAtBattle.TryGetValue(id, out int battle) ? battle : 0).ToArray();
    }

    public static void Restore(IEnumerable<string> encounterIds)
    {
        Restore(encounterIds, null, 0);
    }

    public static void Restore(IEnumerable<string> encounterIds, int[] defeatedAt, int battlesWon)
    {
        defeatedIds.Clear();
        defeatedAtBattle.Clear();
        BattlesWon = battlesWon;

        if (encounterIds == null)
            return;

        int index = 0;
        foreach (string encounterId in encounterIds)
        {
            if (!string.IsNullOrEmpty(encounterId))
            {
                defeatedIds.Add(encounterId);
                defeatedAtBattle[encounterId] = defeatedAt != null && index < defeatedAt.Length ? defeatedAt[index] : 0;
            }

            index++;
        }
    }
}
