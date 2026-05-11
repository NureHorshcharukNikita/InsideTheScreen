using System.Collections.Generic;

public static class CardResolver
{
    public static bool CanResolveAnyTarget(CardData card, BattleTargetingContext context)
    {
        if (card == null)
            return false;
        if (!BattleCondition.AllMet(card.Conditions, context))
            return false;

        return BattleEffectResolver.CanResolveAny(
            card.Effects,
            context,
            ResolveTargets,
            CanUseResolvedTargets);
    }

    public static void Resolve(CardData card, BattleTargetingContext context, BattleActionContext actionContext)
    {
        if (card == null)
            return;
        if (!BattleCondition.AllMet(card.Conditions, context))
            return;

        BattleEffectResolver.Resolve(
            card.Effects,
            context,
            actionContext,
            ResolveTargets,
            CanUseResolvedTargets);
    }

    private static IReadOnlyList<ICombatant> ResolveTargets(CardEffectEntry entry, BattleTargetingContext context)
    {
        if (entry == null || entry.targeting == null)
            return System.Array.Empty<ICombatant>();

        return entry.targeting.ResolveTargets(context);
    }

    private static bool CanUseResolvedTargets(CardEffectEntry entry, BattleTargetingContext context)
    {
        return entry?.targeting != null && entry.targeting.CanUseWithContext(context);
    }
}
