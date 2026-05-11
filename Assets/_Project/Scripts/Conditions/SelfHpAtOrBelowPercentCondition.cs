using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Game/Battle/Conditions/Self HP At Or Below %", fileName = "Cond_SelfHpAtOrBelow")]
public class SelfHpAtOrBelowPercentCondition : BattleCondition
{
    [Range(0f, 1f)]
    [FormerlySerializedAs("threshold01")]
    [SerializeField] private float hpRatioThreshold = 0.5f;

    public override bool IsMet(BattleTargetingContext ctx)
    {
        if (ctx.Self == null || ctx.Self.MaxHealth <= 0)
            return false;

        float ratio = (float)ctx.Self.CurrentHealth / ctx.Self.MaxHealth;
        return ratio <= hpRatioThreshold;
    }
}
