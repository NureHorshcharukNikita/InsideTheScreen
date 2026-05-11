public partial class BattleSystem
{
    private void WireEnemyIntentForBattleStart()
    {
        if (enemyIntentView == null)
            return;

        turnManager.AfterEnemyActed += enemyIntentView.NotifyEnemyActed;
        enemyIntentView.BindEnemy(enemy, deferInitialRevealUntilHandFlyFinishes: true);

        if (handUI != null)
            handUI.DrawFlyAnimationCompleted += OnInitialHandDealFlyCompleteRevealIntent;
        else
            enemyIntentView.ScheduleHandFlyRevealFallback();
    }

    private void OnInitialHandDealFlyCompleteRevealIntent()
    {
        if (handUI != null)
            handUI.DrawFlyAnimationCompleted -= OnInitialHandDealFlyCompleteRevealIntent;

        enemyIntentView?.NotifyHandDealFlyFinished();
    }

    private void OnDestroy()
    {
        UnwireEnemyIntentEvents();
    }

    private void UnwireEnemyIntentEvents()
    {
        if (handUI != null)
            handUI.DrawFlyAnimationCompleted -= OnInitialHandDealFlyCompleteRevealIntent;

        if (turnManager != null && enemyIntentView != null)
            turnManager.AfterEnemyActed -= enemyIntentView.NotifyEnemyActed;
    }
}
