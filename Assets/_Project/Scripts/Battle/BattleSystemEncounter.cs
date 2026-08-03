using UnityEngine;

internal sealed class BattleEncounterResolver
{
    public EnemyCharacter ResolveEnemy(EnemyCharacter fallbackEnemy)
    {
        if (fallbackEnemy == null)
            return null;

        GameObject prefab = PendingBattleEnemy.ConsumeBattlePrefab();

        if (prefab == null)
            return fallbackEnemy;

        EnemyCharacter template = prefab.GetComponent<EnemyCharacter>();
        if (template == null)
        {
            DevLog.Log($"{nameof(BattleSystem)}: battle enemy prefab has no {nameof(EnemyCharacter)}.");
            return fallbackEnemy;
        }

        fallbackEnemy.ApplyEncounterTemplate(template);
        if (PendingBattleEnemy.TryGetCurrentEnemyHealth(out int enemyHealth))
            fallbackEnemy.SetHealth(enemyHealth);

        return fallbackEnemy;
    }
}
