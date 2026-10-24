using System.Collections.Generic;
using System.Linq;

// The encounters the player has beaten, after how many won battles and when (WorldClock): an enemy
// that respawns comes back once the player has won enough other battles since, or a while has
// passed (EnemyEncounter).
public static class DefeatedEncounters
{
    private static readonly HashSet<string> defeatedIds = new();
    private static readonly Dictionary<string, int> defeatedAtBattle = new();
    private static readonly Dictionary<string, float> defeatedAtSeconds = new();

    // Battles won this game, counting every victory.
    public static int BattlesWon { get; private set; }

    public static void MarkDefeated(string encounterId)
    {
        BattlesWon++;
        if (string.IsNullOrEmpty(encounterId))
            return;

        defeatedIds.Add(encounterId);
        defeatedAtBattle[encounterId] = BattlesWon;
        defeatedAtSeconds[encounterId] = WorldClock.PlaySeconds;
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

    // How long the player has explored since this encounter was beaten.
    public static float SecondsSince(string encounterId)
    {
        return defeatedAtSeconds.TryGetValue(encounterId ?? string.Empty, out float seconds)
            ? WorldClock.PlaySeconds - seconds
            : float.MaxValue;
    }

    // Encounters that came and went, e.g. ambushes on the forest roads: beaten ones are not kept.
    public static void ForgetAll(string idPrefix)
    {
        foreach (string encounterId in defeatedIds.Where(id => id.StartsWith(idPrefix)).ToArray())
            Forget(encounterId);
    }

    // The encounter is back on the map, as if never beaten.
    public static void Forget(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
            return;

        defeatedIds.Remove(encounterId);
        defeatedAtBattle.Remove(encounterId);
        defeatedAtSeconds.Remove(encounterId);
    }

    public static bool HasAny()
    {
        return defeatedIds.Count > 0;
    }

    public static void Clear()
    {
        defeatedIds.Clear();
        defeatedAtBattle.Clear();
        defeatedAtSeconds.Clear();
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

    // When each of ToArray's encounters was beaten, in WorldClock seconds.
    public static float[] DefeatedAtSecondsToArray()
    {
        return defeatedIds.Select(id => defeatedAtSeconds.TryGetValue(id, out float seconds) ? seconds : 0f).ToArray();
    }

    public static void Restore(IEnumerable<string> encounterIds)
    {
        Restore(encounterIds, null, 0, null);
    }

    public static void Restore(IEnumerable<string> encounterIds, int[] defeatedAt, int battlesWon, float[] defeatedAtTime)
    {
        defeatedIds.Clear();
        defeatedAtBattle.Clear();
        defeatedAtSeconds.Clear();
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
                defeatedAtSeconds[encounterId] = defeatedAtTime != null && index < defeatedAtTime.Length ? defeatedAtTime[index] : 0f;
            }

            index++;
        }
    }
}
