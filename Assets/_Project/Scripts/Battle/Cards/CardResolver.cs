using System.Collections.Generic;
using UnityEngine;

public static partial class CardResolver
{
    private readonly struct ResolvedEntry
    {
        public ResolvedEntry(CardEffectEntry entry, IReadOnlyList<ICombatant> targets)
        {
            Entry = entry;
            Targets = targets;
        }

        public CardEffectEntry Entry { get; }
        public IReadOnlyList<ICombatant> Targets { get; }
    }

    public static bool CanResolveAnyTarget(CardData card, BattleTargetingContext context)
    {
        if (card == null)
            return false;
        if (!BattleCondition.AllMet(card.Conditions, context))
            return false;

        using IEnumerator<ResolvedEntry> resolved = EnumerateResolvedEntries(card, context).GetEnumerator();
        return resolved.MoveNext();
    }

    public static void Resolve(CardData card, BattleTargetingContext context, BattleActionContext actionContext)
    {
        if (card == null)
            return;
        if (!BattleCondition.AllMet(card.Conditions, context))
            return;

        BattleActionContext runtime = actionContext ?? BattleActionContext.CreateDefault();

        foreach (ResolvedEntry resolved in EnumerateResolvedEntries(card, context))
        {
            if (resolved.Entry.applyChance < 1f && Random.value > resolved.Entry.applyChance)
                continue;

            foreach (ICombatant effectTarget in resolved.Targets)
            {
                if (effectTarget == null)
                    continue;

                resolved.Entry.effect.Execute(context.Self, effectTarget, resolved.Entry.value, runtime);
            }
        }
    }

    private static IEnumerable<ResolvedEntry> EnumerateResolvedEntries(CardData card, BattleTargetingContext context)
    {
        foreach (CardEffectEntry entry in card.Effects)
        {
            if (entry?.effect == null || entry.targeting == null)
                continue;
            if (!BattleCondition.AllMet(entry.conditions, context))
                continue;

            IReadOnlyList<ICombatant> targets = ResolveTargets(entry, context);
            if (targets == null || targets.Count == 0)
                continue;
            if (!SelectionMatchesProfile(entry.targeting, context))
                continue;

            yield return new ResolvedEntry(entry, targets);
        }
    }

    private static IReadOnlyList<ICombatant> ResolveTargets(CardEffectEntry entry, BattleTargetingContext context)
    {
        if (entry == null || entry.targeting == null)
            return System.Array.Empty<ICombatant>();

        return entry.targeting.ResolveTargets(context);
    }

}
