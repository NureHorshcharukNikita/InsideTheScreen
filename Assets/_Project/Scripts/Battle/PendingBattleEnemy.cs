using UnityEngine;

public static class PendingBattleEnemy
{
    private static GameObject _pendingPrefab;
    private static GameObject _lastEncounterPrefab;

    public static string ReturnSceneName { get; private set; } = SceneNames.Exploration;

    public static void RegisterEncounterStart(GameObject battleEnemyPrefab, string returnSceneName = null)
    {
        _pendingPrefab = battleEnemyPrefab;
        if (battleEnemyPrefab != null)
            _lastEncounterPrefab = battleEnemyPrefab;

        if (!string.IsNullOrEmpty(returnSceneName))
            ReturnSceneName = returnSceneName;
    }

    public static bool TryConsumeBattlePrefab(out GameObject prefab)
    {
        prefab = _pendingPrefab;
        _pendingPrefab = null;
        return prefab != null;
    }

    public static GameObject LastEncounterEnemyPrefab => _lastEncounterPrefab;

    public static void ClearSession()
    {
        _pendingPrefab = null;
        _lastEncounterPrefab = null;
        ReturnSceneName = SceneNames.Exploration;
    }
}
