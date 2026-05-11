using UnityEngine;

public partial class BattleSystem
{
    private static EnemyCharacter ResolveEnemyFromPendingEncounter(EnemyCharacter defaultEnemy)
    {
        if (defaultEnemy == null)
            return defaultEnemy;

        GameObject prefab = null;
        if (PendingBattleEnemy.TryConsumeBattlePrefab(out GameObject pending))
            prefab = pending;
        else if (PendingBattleEnemy.LastEncounterEnemyPrefab != null)
            prefab = PendingBattleEnemy.LastEncounterEnemyPrefab;

        if (prefab == null)
            return defaultEnemy;

        EnemyCharacter template = prefab.GetComponent<EnemyCharacter>();
        if (template == null)
        {
            DevLog.Log($"{nameof(BattleSystem)}: battle enemy prefab has no {nameof(EnemyCharacter)}.");
            return defaultEnemy;
        }

        defaultEnemy.ApplyEncounterTemplate(template);
        return defaultEnemy;
    }
}
