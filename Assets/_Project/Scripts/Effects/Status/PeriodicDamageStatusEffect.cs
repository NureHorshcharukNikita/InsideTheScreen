using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Statuses/Periodic Damage")]
public class PeriodicDamageStatusEffect : StatusEffect
{
    [SerializeField] private BattleStatusTiming tickTiming = BattleStatusTiming.TurnEnd;

    public override void Tick(ActiveStatusEffect status, ICombatant target, BattleStatusTiming timing, BattleActionContext actionContext)
    {
        if (status == null || target == null || actionContext == null || timing != tickTiming)
            return;

        int damage = status.Value * Mathf.Max(status.Stacks, 1);
        actionContext.Damage.ApplyDamage(status.Source, target, damage);
    }
}
