using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Effects/Apply Status")]
public class ApplyStatusEffect : BattleEffect
{
    public override void Execute(ICombatant source, ICombatant target, int value, BattleActionContext actionContext)
    {
    }

    public override void Execute(ICombatant source, ICombatant target, IBattleEffectSpec spec, BattleActionContext actionContext)
    {
        if (target == null || actionContext == null || spec?.Status == null)
            return;

        int duration = spec?.Duration ?? 0;
        int stacks = Mathf.Max(spec?.Stacks ?? 1, 1);
        int value = spec?.Value ?? 0;
        actionContext.Status.ApplyStatus(source, target, spec.Status, duration, stacks, value, actionContext);
    }
}
