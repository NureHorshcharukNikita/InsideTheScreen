using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

internal static class EnemyAbilityPicker
{
    // filter: extra rule for this pick, e.g. "fits in the action points left this turn".
    public static EnemyAbilityData PickWeighted(
        IReadOnlyList<EnemyAbilityData> abilities,
        BattleTargetingContext targetingContext,
        EnemyAbilityStateTracker abilityStates,
        Func<EnemyAbilityData, bool> filter = null)
    {
        if (abilities == null || abilities.Count == 0 || abilityStates == null)
            return null;

        int totalWeight = GetTotalWeight(abilities, targetingContext, abilityStates, filter);
        if (totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        int accumulatedWeight = 0;

        for (int i = 0; i < abilities.Count; i++)
        {
            EnemyAbilityData ability = abilities[i];
            if (!IsCandidate(ability, targetingContext, abilityStates, filter))
                continue;

            accumulatedWeight += GetWeight(ability);
            if (roll < accumulatedWeight)
                return ability;
        }

        return null;
    }

    private static int GetTotalWeight(
        IReadOnlyList<EnemyAbilityData> abilities,
        BattleTargetingContext targetingContext,
        EnemyAbilityStateTracker abilityStates,
        Func<EnemyAbilityData, bool> filter)
    {
        int total = 0;
        for (int i = 0; i < abilities.Count; i++)
        {
            EnemyAbilityData ability = abilities[i];
            if (IsCandidate(ability, targetingContext, abilityStates, filter))
                total += GetWeight(ability);
        }

        return total;
    }

    private static bool IsCandidate(
        EnemyAbilityData ability,
        BattleTargetingContext targetingContext,
        EnemyAbilityStateTracker abilityStates,
        Func<EnemyAbilityData, bool> filter)
    {
        return abilityStates.CanUse(ability, targetingContext) && (filter == null || filter(ability));
    }

    private static int GetWeight(EnemyAbilityData ability)
    {
        return Mathf.Max(1, ability.selectionWeight);
    }
}
