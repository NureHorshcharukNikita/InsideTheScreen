using System.Collections.Generic;

internal static class EnemyAbilityTargetPreview
{
    public static Character FindPrimaryTarget(EnemyAbilityData ability, BattleTargetingContext context)
    {
        if (ability?.effects == null || context.Self == null)
            return null;

        foreach (EnemyAbilityEffectSpec spec in ability.effects)
        {
            if (spec == null)
                continue;

            IReadOnlyList<ICombatant> targets = EnemyAbilityExecutor.ResolveTargets(spec, context);
            if (targets != null && targets.Count > 0)
                return targets[0] as Character;
        }

        return null;
    }
}
