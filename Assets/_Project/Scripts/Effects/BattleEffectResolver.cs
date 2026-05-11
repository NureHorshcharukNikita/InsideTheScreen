using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleEffectResolver
{
    public static bool CanResolveAny<TSpec>(
        IReadOnlyList<TSpec> specs,
        BattleTargetingContext context,
        Func<TSpec, BattleTargetingContext, IReadOnlyList<ICombatant>> resolveTargets,
        Func<TSpec, BattleTargetingContext, bool> canUseTargets = null)
        where TSpec : class, IBattleEffectSpec
    {
        foreach (ResolvedEffect<TSpec> _ in EnumerateResolved(specs, context, resolveTargets, canUseTargets))
            return true;

        return false;
    }

    public static void Resolve<TSpec>(
        IReadOnlyList<TSpec> specs,
        BattleTargetingContext context,
        BattleActionContext actionContext,
        Func<TSpec, BattleTargetingContext, IReadOnlyList<ICombatant>> resolveTargets,
        Func<TSpec, BattleTargetingContext, bool> canUseTargets = null)
        where TSpec : class, IBattleEffectSpec
    {
        BattleActionContext runtime = actionContext ?? BattleActionContext.CreateDefault();

        foreach (ResolvedEffect<TSpec> resolved in EnumerateResolved(specs, context, resolveTargets, canUseTargets))
        {
            if (resolved.Spec.ApplyChance < 1f && UnityEngine.Random.value > resolved.Spec.ApplyChance)
                continue;

            foreach (ICombatant target in resolved.Targets)
            {
                if (target == null)
                    continue;

                resolved.Spec.Effect.Execute(context.Self, target, resolved.Spec.Value, runtime);
            }
        }
    }

    private static IEnumerable<ResolvedEffect<TSpec>> EnumerateResolved<TSpec>(
        IReadOnlyList<TSpec> specs,
        BattleTargetingContext context,
        Func<TSpec, BattleTargetingContext, IReadOnlyList<ICombatant>> resolveTargets,
        Func<TSpec, BattleTargetingContext, bool> canUseTargets)
        where TSpec : class, IBattleEffectSpec
    {
        if (specs == null || resolveTargets == null)
            yield break;

        foreach (TSpec spec in specs)
        {
            if (spec?.Effect == null)
                continue;
            if (!BattleCondition.AllMet(spec.Conditions, context))
                continue;

            IReadOnlyList<ICombatant> targets = resolveTargets(spec, context);
            if (targets == null || targets.Count == 0)
                continue;
            if (canUseTargets != null && !canUseTargets(spec, context))
                continue;

            yield return new ResolvedEffect<TSpec>(spec, targets);
        }
    }

    private readonly struct ResolvedEffect<TSpec>
    {
        public ResolvedEffect(TSpec spec, IReadOnlyList<ICombatant> targets)
        {
            Spec = spec;
            Targets = targets;
        }

        public TSpec Spec { get; }
        public IReadOnlyList<ICombatant> Targets { get; }
    }
}
