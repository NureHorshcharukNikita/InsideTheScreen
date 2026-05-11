using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Conditions/Self HP At Or Below %", fileName = "Cond_SelfHpAtOrBelow")]
public class SelfHpAtOrBelowPercentCondition : BattleCondition
{
    [Range(0f, 1f)]
    [SerializeField] private float hpRatioThreshold = 0.5f;

    public override bool IsMet(BattleTargetingContext context)
    {
        if (context.Self == null || context.Self.MaxHealth <= 0)
            return false;

        float ratio = (float)context.Self.CurrentHealth / context.Self.MaxHealth;
        return ratio <= hpRatioThreshold;
    }
}
