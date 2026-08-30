using System.Collections.Generic;

internal static class EnemyAbilityTargetPreview
{
    public static Character FindPrimaryTarget(EnemyAbilityData ability, BattleTargetingContext context)
    {
        if (ability?.instantEffects == null || context.Self == null)
            return null;

        foreach (EnemyAbilityInstantEffectSpec spec in ability.instantEffects)
        {
            if (spec == null)
                continue;

            IReadOnlyList<ICombatant> targets = EnemyAbilityExecutor.ResolveTargets(spec, context);
            if (targets != null && targets.Count > 0)
                return targets[0] as Character;
        }

        foreach (EnemyAbilityStatusApplicationSpec spec in ability.statusApplications)
        {
            if (spec == null)
                continue;

            IReadOnlyList<ICombatant> targets = EnemyAbilityExecutor.ResolveStatusTargets(spec, context);
            if (targets != null && targets.Count > 0)
                return targets[0] as Character;
        }

        return null;
    }
}
