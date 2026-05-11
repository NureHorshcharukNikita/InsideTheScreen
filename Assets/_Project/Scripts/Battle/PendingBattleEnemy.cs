using UnityEngine;

public static class PendingBattleEnemy
{
    private static GameObject _battleEnemyPrefab;
    private static GameObject _currentBattleEnemyPrefab;

    public static string EncounterId { get; private set; }

    public static void RegisterEncounterStart(GameObject battleEnemyPrefab, string encounterId)
    {
        _battleEnemyPrefab = battleEnemyPrefab;
        _currentBattleEnemyPrefab = battleEnemyPrefab;
        EncounterId = encounterId;
    }

    public static GameObject ConsumeBattlePrefab()
    {
        GameObject prefab = _battleEnemyPrefab != null ? _battleEnemyPrefab : _currentBattleEnemyPrefab;
        _battleEnemyPrefab = null;
        return prefab;
    }

    public static void ClearSession()
    {
        _battleEnemyPrefab = null;
        _currentBattleEnemyPrefab = null;
        EncounterId = null;
    }
}
