using System.Collections.Generic;

public static class CardResolver
{
    public static bool CanResolveAnyTarget(CardData card, BattleTargetingContext context)
    {
        if (card == null)
            return false;
        if (!BattleCondition.AllMet(card.Conditions, context))
            return false;

        return InstantEffectResolver.CanResolveAny(
                card.InstantEffects,
                context,
                ResolveTargets,
                CanUseResolvedTargets)
            || StatusApplicationResolver.CanResolveAny(
                card.StatusApplications,
                context,
                ResolveStatusTargets,
                CanUseResolvedStatusTargets);
    }

    public static void Resolve(CardData card, BattleTargetingContext context, BattleActionContext actionContext)
    {
        if (card == null)
            return;
        if (!BattleCondition.AllMet(card.Conditions, context))
            return;

        InstantEffectResolver.Resolve(
            card.InstantEffects,
            context,
            actionContext,
            ResolveTargets,
            CanUseResolvedTargets);

        StatusApplicationResolver.Resolve(
            card.StatusApplications,
            context,
            actionContext,
            ResolveStatusTargets,
            CanUseResolvedStatusTargets);
    }

    private static IReadOnlyList<ICombatant> ResolveTargets(CardInstantEffectEntry entry, BattleTargetingContext context)
    {
        if (entry == null || entry.targeting == null)
            return System.Array.Empty<ICombatant>();

        return entry.targeting.ResolveTargets(context);
    }

    private static bool CanUseResolvedTargets(CardInstantEffectEntry entry, BattleTargetingContext context)
    {
        return entry?.targeting != null && entry.targeting.CanUseWithContext(context);
    }

    private static IReadOnlyList<ICombatant> ResolveStatusTargets(CardStatusApplicationEntry entry, BattleTargetingContext context)
    {
        if (entry == null || entry.targeting == null)
            return System.Array.Empty<ICombatant>();

        return entry.targeting.ResolveTargets(context);
    }

    private static bool CanUseResolvedStatusTargets(CardStatusApplicationEntry entry, BattleTargetingContext context)
    {
        return entry?.targeting != null && entry.targeting.CanUseWithContext(context);
    }
}
