using UnityEngine;

// For cards played on a chosen fighter, e.g. a bandage on Hideshi: his health counts, not the
// player's. Without a chosen fighter, the source's.
[CreateAssetMenu(menuName = "Game/Battle/Conditions/Target HP At Or Below %", fileName = "Cond_TargetHpAtOrBelow")]
public class TargetHpAtOrBelowPercentCondition : BattleCondition
{
    [Range(0f, 1f)]
    [SerializeField] private float hpRatioThreshold = 0.5f;

    public override bool IsMet(BattleTargetingContext context)
    {
        ICombatant target = context.SelectedTarget ?? context.Self;
        if (target == null || target.MaxHealth <= 0)
            return false;

        float ratio = (float)target.CurrentHealth / target.MaxHealth;
        return ratio <= hpRatioThreshold;
    }
}
