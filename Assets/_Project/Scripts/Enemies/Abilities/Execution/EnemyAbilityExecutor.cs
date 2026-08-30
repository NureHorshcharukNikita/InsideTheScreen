using System.Collections.Generic;

public static class EnemyAbilityExecutor
{
    public static IReadOnlyList<ICombatant> ResolveTargets(EnemyAbilityInstantEffectSpec spec, BattleTargetingContext targetingContext)
    {
        if (targetingContext.Self == null || spec == null || spec.targeting == null)
            return System.Array.Empty<ICombatant>();

        return spec.targeting.ResolveTargets(targetingContext);
    }

    public static IReadOnlyList<ICombatant> ResolveStatusTargets(EnemyAbilityStatusApplicationSpec spec, BattleTargetingContext targetingContext)
    {
        if (targetingContext.Self == null || spec == null || spec.targeting == null)
            return System.Array.Empty<ICombatant>();

        return spec.targeting.ResolveTargets(targetingContext);
    }

    public static void ApplyAbility(EnemyAbilityData ability, BattleTargetingContext targetingContext, BattleActionContext actionContext = null)
    {
        if (ability == null || targetingContext.Self == null || ability.instantEffects == null)
            return;

        InstantEffectResolver.Resolve(
            ability.instantEffects,
            targetingContext,
            actionContext,
            ResolveTargets);

        StatusApplicationResolver.Resolve(
            ability.statusApplications,
            targetingContext,
            actionContext,
            ResolveStatusTargets);
    }

}
