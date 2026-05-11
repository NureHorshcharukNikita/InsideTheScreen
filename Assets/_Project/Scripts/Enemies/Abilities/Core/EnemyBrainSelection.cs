using System.Collections.Generic;
using UnityEngine;

internal static class EnemyBrainSelection
{
    public static EnemyAbilityData PickWeighted(
        IReadOnlyList<EnemyAbilityData> pool,
        EnemyAbilityBattleContext abilityContext,
        Dictionary<EnemyAbilityData, EnemyBrainRuntime.AbilityRuntimeState> runtime)
    {
        BattleTargetingContext targetingContext = EnemyAbilityExecutor.BuildTargetingContext(abilityContext);
        int total = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            EnemyAbilityData ability = pool[i];
            if (ability != null && EnemyBrainRuntime.IsAbilityAvailable(ability, targetingContext, runtime))
                total += Mathf.Max(1, ability.selectionWeight);
        }

        if (total <= 0)
            return null;

        int roll = Random.Range(0, total);
        int accumulatedWeight = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            EnemyAbilityData ability = pool[i];
            if (ability == null)
                continue;
            if (!EnemyBrainRuntime.IsAbilityAvailable(ability, targetingContext, runtime))
                continue;

            accumulatedWeight += Mathf.Max(1, ability.selectionWeight);
            if (roll < accumulatedWeight)
                return ability;
        }

        return null;
    }

    public static Character ResolvePrimaryTargetForUi(EnemyAbilityData ability, EnemyAbilityBattleContext abilityContext)
    {
        if (ability?.effects == null || abilityContext == null)
            return null;

        foreach (EnemyAbilityEffectSpec spec in ability.effects)
        {
            if (spec == null)
                continue;

            IReadOnlyList<Character> targets = EnemyAbilityExecutor.ResolveTargets(spec, abilityContext);
            if (targets != null && targets.Count > 0)
                return targets[0];
        }

        return null;
    }
}
