using UnityEngine;

// A hit that is stronger against a target with a status, e.g. an arrow or a torch on a burning
// foe: Bonus Damage more, and Bonus Share of the hit on top (0.5 = half again).
[CreateAssetMenu(menuName = "Game/Battle/Instant Effects/Damage With Bonus")]
public class BonusDamageEffect : DamageEffect
{
    [Header("Bonus")]
    [Tooltip("The target has this status, e.g. Burning.")]
    [SerializeField] private BattleStatus against;
    [SerializeField, Min(0)] private int bonusDamage;
    [SerializeField, Min(0f)] private float bonusShare;

    public override void Execute(ICombatant source, ICombatant target, int value, BattleActionContext actionContext)
    {
        if (BattleStatusQuery.Has(target, against))
            value += bonusDamage + Mathf.RoundToInt(value * bonusShare);

        base.Execute(source, target, value, actionContext);
    }
}
