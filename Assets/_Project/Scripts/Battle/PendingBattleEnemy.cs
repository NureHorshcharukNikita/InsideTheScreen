public static class PendingBattleEnemy
{
    private static EnemyData _enemyData;
    private static EnemyData _currentEnemyData;
    private static int _currentEnemyHealth;
    private static bool _hasCurrentEnemyHealth;

    public static string EncounterId { get; private set; }

    public static void RegisterEncounterStart(EnemyData enemyData, string encounterId)
    {
        _enemyData = enemyData;
        _currentEnemyData = enemyData;
        _currentEnemyHealth = default;
        _hasCurrentEnemyHealth = false;
        EncounterId = encounterId;
    }

    public static EnemyData ConsumeEnemyData()
    {
        EnemyData data = _enemyData != null ? _enemyData : _currentEnemyData;
        _enemyData = null;
        return data;
    }

    public static void SaveCurrentEnemyHealth(int health)
    {
        _currentEnemyHealth = health;
        _hasCurrentEnemyHealth = true;
    }

    public static bool TryGetCurrentEnemyHealth(out int health)
    {
        health = _currentEnemyHealth;
        return _hasCurrentEnemyHealth;
    }

    public static void RestoreSession(string encounterId, bool hasEnemyHealth, int enemyHealth)
    {
        _enemyData = null;
        _currentEnemyData = null;
        EncounterId = encounterId;
        _hasCurrentEnemyHealth = hasEnemyHealth;
        _currentEnemyHealth = enemyHealth;
    }

    public static void ClearSession()
    {
        _enemyData = null;
        _currentEnemyData = null;
        _currentEnemyHealth = default;
        _hasCurrentEnemyHealth = false;
        EncounterId = null;
    }
}
