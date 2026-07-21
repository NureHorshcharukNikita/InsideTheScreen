using UnityEngine;

public static class PendingBattleEnemy
{
    private static GameObject _battleEnemyPrefab;
    private static GameObject _currentBattleEnemyPrefab;
    private static int _currentEnemyHealth;
    private static bool _hasCurrentEnemyHealth;

    public static string EncounterId { get; private set; }

    public static void RegisterEncounterStart(GameObject battleEnemyPrefab, string encounterId)
    {
        _battleEnemyPrefab = battleEnemyPrefab;
        _currentBattleEnemyPrefab = battleEnemyPrefab;
        _currentEnemyHealth = default;
        _hasCurrentEnemyHealth = false;
        EncounterId = encounterId;
    }

    public static GameObject ConsumeBattlePrefab()
    {
        GameObject prefab = _battleEnemyPrefab != null ? _battleEnemyPrefab : _currentBattleEnemyPrefab;
        _battleEnemyPrefab = null;
        return prefab;
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
        _battleEnemyPrefab = null;
        _currentBattleEnemyPrefab = null;
        EncounterId = encounterId;
        _hasCurrentEnemyHealth = hasEnemyHealth;
        _currentEnemyHealth = enemyHealth;
    }

    public static void ClearSession()
    {
        _battleEnemyPrefab = null;
        _currentBattleEnemyPrefab = null;
        _currentEnemyHealth = default;
        _hasCurrentEnemyHealth = false;
        EncounterId = null;
    }
}
