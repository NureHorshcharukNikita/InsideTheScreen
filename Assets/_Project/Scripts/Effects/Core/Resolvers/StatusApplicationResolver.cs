using System;
using System.Collections.Generic;
using UnityEngine;

public static class StatusApplicationResolver
{
    public static bool CanResolveAny<TSpec>(
        IReadOnlyList<TSpec> specs,
        BattleTargetingContext context,
        Func<TSpec, BattleTargetingContext, IReadOnlyList<ICombatant>> resolveTargets,
        Func<TSpec, BattleTargetingContext, bool> canUseTargets = null)
        where TSpec : class, IStatusApplicationSpec
    {
        foreach (ResolvedStatusApplication<TSpec> _ in EnumerateResolved(specs, context, resolveTargets, canUseTargets))
            return true;

        return false;
    }

    public static void Resolve<TSpec>(
        IReadOnlyList<TSpec> specs,
        BattleTargetingContext context,
        BattleActionContext actionContext,
        Func<TSpec, BattleTargetingContext, IReadOnlyList<ICombatant>> resolveTargets,
        Func<TSpec, BattleTargetingContext, bool> canUseTargets = null)
        where TSpec : class, IStatusApplicationSpec
    {
        BattleActionContext runtime = actionContext ?? BattleActionContext.CreateDefault();

        foreach (ResolvedStatusApplication<TSpec> resolved in EnumerateResolved(specs, context, resolveTargets, canUseTargets))
        {
            foreach (ICombatant target in resolved.Targets)
            {
                if (target == null)
                    continue;

                runtime.Status.ApplyStatus(
                    context.Self,
                    target,
                    resolved.Spec.Status,
                    resolved.Spec.Duration,
                    Mathf.Max(resolved.Spec.Stacks, 1),
                    resolved.Spec.Value,
                    ResolveReapplyMode(resolved.Spec),
                    runtime);
            }
        }
    }

    private static BattleStatusReapplyMode ResolveReapplyMode(IStatusApplicationSpec spec)
    {
        if (spec?.Status == null)
            return BattleStatusReapplyMode.AddStacks;

        return spec.ReapplyMode switch
        {
            StatusApplicationReapplyMode.AddDuration => BattleStatusReapplyMode.AddDuration,
            StatusApplicationReapplyMode.AddStacksAndDuration => BattleStatusReapplyMode.AddStacksAndDuration,
            StatusApplicationReapplyMode.AddStacks => BattleStatusReapplyMode.AddStacks,
            _ => spec.Status.ReapplyMode
        };
    }

    private static IEnumerable<ResolvedStatusApplication<TSpec>> EnumerateResolved<TSpec>(
        IReadOnlyList<TSpec> specs,
        BattleTargetingContext context,
        Func<TSpec, BattleTargetingContext, IReadOnlyList<ICombatant>> resolveTargets,
        Func<TSpec, BattleTargetingContext, bool> canUseTargets)
        where TSpec : class, IStatusApplicationSpec
    {
        if (specs == null || resolveTargets == null)
            yield break;

        foreach (TSpec spec in specs)
        {
            if (spec?.Status == null)
                continue;
            if (UnityEngine.Random.value > Mathf.Clamp01(spec.ApplyChance))
                continue;
            if (!BattleEffectConditionSpec.AllMet(spec.Conditions, context))
                continue;

            IReadOnlyList<ICombatant> targets = resolveTargets(spec, context);
            if (targets == null || targets.Count == 0)
                continue;
            if (canUseTargets != null && !canUseTargets(spec, context))
                continue;

            yield return new ResolvedStatusApplication<TSpec>(spec, targets);
        }
    }

    private readonly struct ResolvedStatusApplication<TSpec>
    {
        public ResolvedStatusApplication(TSpec spec, IReadOnlyList<ICombatant> targets)
        {
            Spec = spec;
            Targets = targets;
        }

        public TSpec Spec { get; }
        public IReadOnlyList<ICombatant> Targets { get; }
    }
}
