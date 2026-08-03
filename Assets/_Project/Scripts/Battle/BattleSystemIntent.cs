internal sealed class BattleSystemIntentBinder
{
    private readonly EnemyIntentView enemyIntentView;

    public BattleSystemIntentBinder(EnemyIntentView enemyIntentView)
    {
        this.enemyIntentView = enemyIntentView;
    }

    public void WireForBattleStart(TurnManager turnManager, EnemyCharacter enemy)
    {
        if (enemyIntentView == null)
            return;

        enemyIntentView.BindEnemy(enemy, deferInitialRevealUntilHandFlyFinishes: true);
    }

    public void Unwire()
    {
    }
}
