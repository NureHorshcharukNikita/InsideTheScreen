internal sealed class BattleEncounterResolver
{
    public EnemyCharacter ResolveEnemy(EnemyCharacter fallbackEnemy)
    {
        if (fallbackEnemy == null)
            return null;

        EnemyData enemyData = PendingBattleEnemy.ConsumeEnemyData();

        if (enemyData == null)
            return fallbackEnemy;

        fallbackEnemy.ApplyData(enemyData);
        if (PendingBattleEnemy.TryGetCurrentEnemyHealth(out int enemyHealth))
            fallbackEnemy.SetHealth(enemyHealth);

        return fallbackEnemy;
    }
}
