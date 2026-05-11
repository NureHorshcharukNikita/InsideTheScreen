using System.Collections.Generic;
using UnityEngine;

internal static class EnemyAbilityPicker
{
    public static EnemyAbilityData PickWeighted(
        IReadOnlyList<EnemyAbilityData> abilities,
        BattleTargetingContext targetingContext,
        EnemyAbilityStateTracker abilityStates)
    {
        if (abilities == null || abilities.Count == 0 || abilityStates == null)
            return null;

        int totalWeight = GetTotalWeight(abilities, targetingContext, abilityStates);
        if (totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        int accumulatedWeight = 0;

        for (int i = 0; i < abilities.Count; i++)
        {
            EnemyAbilityData ability = abilities[i];
            if (!abilityStates.CanUse(ability, targetingContext))
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
        EnemyAbilityStateTracker abilityStates)
    {
        int total = 0;
        for (int i = 0; i < abilities.Count; i++)
        {
            EnemyAbilityData ability = abilities[i];
            if (abilityStates.CanUse(ability, targetingContext))
                total += GetWeight(ability);
        }

        return total;
    }

    private static int GetWeight(EnemyAbilityData ability)
    {
        return Mathf.Max(1, ability.selectionWeight);
    }
}
