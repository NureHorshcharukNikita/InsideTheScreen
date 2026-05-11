using System.Collections.Generic;

public static class EnemyAbilityExecutor
{
    public static IReadOnlyList<ICombatant> ResolveTargets(EnemyAbilityEffectSpec spec, BattleTargetingContext targetingContext)
    {
        if (targetingContext.Self == null || spec == null || spec.targeting == null)
            return System.Array.Empty<ICombatant>();

        return spec.targeting.ResolveTargets(targetingContext);
    }

    public static void ApplyAbility(EnemyAbilityData ability, BattleTargetingContext targetingContext, BattleActionContext actionContext = null)
    {
        if (ability == null || targetingContext.Self == null || ability.effects == null)
            return;

        BattleEffectResolver.Resolve(
            ability.effects,
            targetingContext,
            actionContext,
            ResolveTargets);
    }

}
