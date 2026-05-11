using System.Collections.Generic;
using UnityEngine;

public partial class EnemyBrain
{
    private static EnemyAbilityData PickWeighted(
        IReadOnlyList<EnemyAbilityData> pool,
        EnemyAbilityBattleContext ctx,
        Dictionary<EnemyAbilityData, AbilityRuntimeState> runtime)
    {
        BattleTargetingContext targetingCtx = EnemyAbilityExecutor.BuildTargetingContext(ctx);
        int total = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            EnemyAbilityData a = pool[i];
            if (a != null && IsAbilityAvailable(a, targetingCtx, runtime))
                total += Mathf.Max(1, a.selectionWeight);
        }

        if (total <= 0)
            return null;

        int roll = Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            EnemyAbilityData a = pool[i];
            if (a == null)
                continue;
            if (!IsAbilityAvailable(a, targetingCtx, runtime))
                continue;

            acc += Mathf.Max(1, a.selectionWeight);
            if (roll < acc)
                return a;
        }

        return null;
    }

    private static Character ResolvePrimaryTargetForUi(EnemyAbilityData ability, EnemyAbilityBattleContext ctx)
    {
        if (ability?.effects == null || ctx == null)
            return null;

        foreach (EnemyAbilityEffectSpec spec in ability.effects)
        {
            if (spec == null)
                continue;

            IReadOnlyList<Character> targets = EnemyAbilityExecutor.ResolveTargets(spec, ctx);
            if (targets != null && targets.Count > 0)
                return targets[0];
        }

        return null;
    }
}
