internal sealed class BattleSystemIntentBinder
{
    private readonly HandUI handUI;
    private readonly EnemyIntentView enemyIntentView;

    private TurnManager turnManager;

    public BattleSystemIntentBinder(HandUI handUI, EnemyIntentView enemyIntentView)
    {
        this.handUI = handUI;
        this.enemyIntentView = enemyIntentView;
    }

    public void WireForBattleStart(TurnManager turnManager, EnemyCharacter enemy)
    {
        if (enemyIntentView == null)
            return;

        this.turnManager = turnManager;
        enemyIntentView.BindEnemy(enemy, deferInitialRevealUntilHandFlyFinishes: true);

        if (handUI != null)
            handUI.DrawFlyAnimationCompleted += OnInitialHandDealFlyCompleteRevealIntent;
        else
            enemyIntentView.ScheduleHandFlyRevealFallback();
    }

    public void Unwire()
    {
        if (handUI != null)
            handUI.DrawFlyAnimationCompleted -= OnInitialHandDealFlyCompleteRevealIntent;

        turnManager = null;
    }

    private void OnInitialHandDealFlyCompleteRevealIntent()
    {
        if (handUI != null)
            handUI.DrawFlyAnimationCompleted -= OnInitialHandDealFlyCompleteRevealIntent;

        enemyIntentView?.NotifyHandDealFlyFinished();
    }
}
