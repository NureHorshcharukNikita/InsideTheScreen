using UnityEngine;

// Passes when any living ally of the source, the source included, is at or below the threshold,
// e.g. Hideshi heals only once he or Haru is badly hurt.
[CreateAssetMenu(menuName = "Game/Battle/Conditions/Ally HP At Or Below %", fileName = "Cond_AllyHpAtOrBelow")]
public class AllyHpAtOrBelowPercentCondition : BattleCondition
{
    [Range(0f, 1f)]
    [SerializeField] private float hpRatioThreshold = 0.5f;

    public override bool IsMet(BattleTargetingContext context)
    {
        foreach (ICombatant ally in context.Allies)
        {
            if (ally == null || ally.MaxHealth <= 0)
                continue;

            if ((float)ally.CurrentHealth / ally.MaxHealth <= hpRatioThreshold)
                return true;
        }

        return false;
    }
}
