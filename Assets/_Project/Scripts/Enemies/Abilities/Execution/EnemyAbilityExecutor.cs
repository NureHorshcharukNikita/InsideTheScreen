using System.Collections.Generic;
using UnityEngine;

public static class EnemyAbilityExecutor
{
    public static BattleTargetingContext BuildTargetingContext(EnemyAbilityBattleContext ctx)
    {
        if (ctx == null)
            return new BattleTargetingContext(null, null, null, null);

        return new BattleTargetingContext(
            ctx.Self,
            ctx.Player,
            ToCombatants(ctx.GetAllies()),
            ToCombatants(ctx.GetEnemies()));
    }

    public static IReadOnlyList<Character> ResolveTargets(EnemyAbilityEffectSpec spec, EnemyAbilityBattleContext ctx)
    {
        if (ctx == null || ctx.Self == null || spec == null)
            return System.Array.Empty<Character>();

        if (spec.targeting != null)
        {
            BattleTargetingContext targetingCtx = BuildTargetingContext(ctx);
            IReadOnlyList<ICombatant> rawTargets = spec.targeting.ResolveTargets(targetingCtx);
            return FilterCharacters(rawTargets);
        }

        return PickSingle(ctx.GetEnemies());
    }

    public static void ApplyAbility(EnemyAbilityData ability, EnemyAbilityBattleContext ctx, BattleActionContext actionContext = null)
    {
        if (ability == null || ctx == null || ability.effects == null)
            return;

        BattleActionContext runtime = actionContext ?? BattleActionContext.CreateDefault();
        BattleTargetingContext targetingCtx = BuildTargetingContext(ctx);
        foreach (EnemyAbilityEffectSpec spec in ability.effects)
        {
            if (spec == null)
                continue;

            if (!BattleCondition.AllMet(spec.conditions, targetingCtx))
                continue;

            if (spec.applyChance < 1f && Random.value > spec.applyChance)
                continue;

            IReadOnlyList<Character> targets = ResolveTargets(spec, ctx);
            foreach (Character target in targets)
            {
                if (target == null)
                    continue;

                ApplyEffect(spec, ctx, target, runtime);
            }
        }
    }

    private static IReadOnlyList<ICombatant> ToCombatants(IReadOnlyList<Character> list)
    {
        if (list == null || list.Count == 0)
            return System.Array.Empty<ICombatant>();

        var arr = new ICombatant[list.Count];
        for (int i = 0; i < list.Count; i++)
            arr[i] = list[i] as ICombatant;
        return arr;
    }

    private static IReadOnlyList<Character> FilterCharacters(IReadOnlyList<ICombatant> list)
    {
        if (list == null || list.Count == 0)
            return System.Array.Empty<Character>();

        var result = new List<Character>(list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is Character c)
                result.Add(c);
        }

        return result;
    }

    private static void ApplyEffect(EnemyAbilityEffectSpec spec, EnemyAbilityBattleContext ctx, Character target, BattleActionContext actionContext)
    {
        if (spec == null || target == null)
            return;

        if (spec.effect != null)
        {
            spec.effect.Execute(ctx?.Self, target, spec.amount, actionContext);
            return;
        }

    }

    private static IReadOnlyList<Character> PickSingle(IReadOnlyList<Character> list)
    {
        if (list == null || list.Count == 0)
            return System.Array.Empty<Character>();

        int i = Random.Range(0, list.Count);
        return new[] { list[i] };
    }

}
