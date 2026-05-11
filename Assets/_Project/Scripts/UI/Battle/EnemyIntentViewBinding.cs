using System;
using UnityEngine;

internal sealed class EnemyIntentBrainBinding
{
    private readonly Transform ownerTransform;
    private readonly Action plannedChangedHandler;

    private EnemyBrain brain;
    private EnemyBrain subscribedBrain;
    private Character targetCharacter;

    public EnemyIntentBrainBinding(Transform ownerTransform, EnemyBrain initialBrain, Action plannedChangedHandler)
    {
        this.ownerTransform = ownerTransform;
        this.plannedChangedHandler = plannedChangedHandler;
        brain = initialBrain;
    }

    public EnemyBrain Brain => brain;
    public Character TargetCharacter => targetCharacter;

    public void BindEnemy(EnemyCharacter enemyCharacter)
    {
        Unsubscribe();

        if (enemyCharacter == null)
        {
            brain = null;
            targetCharacter = null;
            return;
        }

        targetCharacter = enemyCharacter;
        brain = enemyCharacter.Brain;
        Subscribe();
    }

    public void ResolveMissingReferences()
    {
        ResolveBrainIfMissing();
        ResolveTargetCharacterIfMissing();
        Subscribe();
    }

    public void Unsubscribe()
    {
        if (subscribedBrain == null)
            return;

        subscribedBrain.PlannedActionChanged -= plannedChangedHandler;
        subscribedBrain = null;
    }

    private void ResolveBrainIfMissing()
    {
        if (brain != null)
            return;

        brain = ownerTransform.GetComponentInParent<EnemyBrain>();
        if (brain == null)
            brain = UnityEngine.Object.FindAnyObjectByType<EnemyBrain>();
    }

    private void ResolveTargetCharacterIfMissing()
    {
        if (targetCharacter != null)
            return;

        ResolveBrainIfMissing();
        if (brain != null)
            targetCharacter = brain.GetComponent<Character>();
    }

    private void Subscribe()
    {
        if (subscribedBrain == brain)
            return;

        Unsubscribe();
        if (brain == null)
            return;

        brain.PlannedActionChanged += plannedChangedHandler;
        subscribedBrain = brain;
    }
}
