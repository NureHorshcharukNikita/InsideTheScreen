using System.Collections.Generic;

// The fight about to start, or being fought: its enemies (one to three), the allies fighting at
// the player's side (up to two, three a side with the player) and the encounter it belongs to.
// Kept for a retry; enemy health is kept for a battle saved halfway.
public static class PendingBattleEnemy
{
    public const int MaxPerSide = 3;

    private static readonly List<EnemyData> enemies = new();
    private static readonly List<EnemyData> allies = new();
    private static int[] currentEnemyHealths;

    public static string EncounterId { get; private set; }
    public static IReadOnlyList<EnemyData> Enemies => enemies;
    public static IReadOnlyList<EnemyData> Allies => allies;
    public static int[] CurrentEnemyHealths => currentEnemyHealths;

    public static void RegisterEncounterStart(
        IEnumerable<EnemyData> encounterEnemies,
        IEnumerable<EnemyData> encounterAllies,
        string encounterId)
    {
        Fill(enemies, encounterEnemies, MaxPerSide);
        Fill(allies, encounterAllies, MaxPerSide - 1);
        currentEnemyHealths = null;
        EncounterId = encounterId;
    }

    public static void SaveCurrentEnemyHealths(int[] healths)
    {
        currentEnemyHealths = healths;
    }

    public static bool TryGetCurrentEnemyHealth(int index, out int health)
    {
        bool hasHealth = currentEnemyHealths != null && index >= 0 && index < currentEnemyHealths.Length;
        health = hasHealth ? currentEnemyHealths[index] : default;
        return hasHealth;
    }

    public static void RestoreSession(string encounterId, int[] enemyHealths)
    {
        enemies.Clear();
        allies.Clear();
        EncounterId = encounterId;
        currentEnemyHealths = enemyHealths;
    }

    public static void ClearSession()
    {
        enemies.Clear();
        allies.Clear();
        currentEnemyHealths = null;
        EncounterId = null;
    }

    private static void Fill(List<EnemyData> list, IEnumerable<EnemyData> source, int max)
    {
        list.Clear();
        if (source == null)
            return;

        foreach (EnemyData data in source)
        {
            if (data != null && list.Count < max)
                list.Add(data);
        }
    }
}
