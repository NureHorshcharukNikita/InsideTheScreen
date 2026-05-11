using UnityEngine;

public partial class EnemyIntentView
{
    public void BindEnemy(EnemyCharacter enemyCharacter, bool deferInitialRevealUntilHandFlyFinishes = false)
    {
        StopAllRevealCoroutines();
        _pendingRevealAfterEnemyAct = false;
        _awaitingHandDealFlyReveal = false;

        UnsubscribeFromBrain();

        if (enemyCharacter == null)
        {
            brain = null;
            targetCharacter = null;
            Refresh();
            return;
        }

        targetCharacter = enemyCharacter;
        brain = enemyCharacter.Brain;
        SubscribeToBrain();

        if (deferInitialRevealUntilHandFlyFinishes)
        {
            _awaitingHandDealFlyReveal = true;
            Refresh(keepVisualHidden: true);
        }
        else
            Refresh();
    }

    private void ResolveBrainIfMissing()
    {
        if (brain != null)
            return;

        brain = GetComponentInParent<EnemyBrain>();
        if (brain == null)
            brain = FindAnyObjectByType<EnemyBrain>();
    }

    private void ResolveTargetCharacterIfMissing()
    {
        if (targetCharacter != null)
            return;

        ResolveBrainIfMissing();
        if (brain != null)
            targetCharacter = brain.GetComponent<Character>();
    }

    private void SubscribeToBrain()
    {
        if (subscribedBrain == brain)
            return;

        UnsubscribeFromBrain();
        if (brain == null)
            return;

        brain.PlannedActionChanged += OnPlannedChanged;
        subscribedBrain = brain;
    }

    private void UnsubscribeFromBrain()
    {
        if (subscribedBrain == null)
            return;

        subscribedBrain.PlannedActionChanged -= OnPlannedChanged;
        subscribedBrain = null;
    }
}
