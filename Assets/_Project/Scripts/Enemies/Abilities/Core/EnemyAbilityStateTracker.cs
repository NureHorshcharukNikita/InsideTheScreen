using System.Collections.Generic;
using UnityEngine;

internal sealed class EnemyAbilityStateTracker
{
    private readonly Dictionary<EnemyAbilityData, EnemyAbilityState> states = new();

    public void Clear()
    {
        states.Clear();
    }

    public bool CanUse(EnemyAbilityData ability, BattleTargetingContext targetingContext)
    {
        if (ability == null)
            return false;

        EnemyAbilityState state = GetState(ability);
        if (state.CooldownRemaining > 0)
            return false;

        if (ability.maxUses >= 0 && state.Uses >= ability.maxUses)
            return false;

        return BattleCondition.AllMet(ability.conditions, targetingContext);
    }

    public void RecordUsed(EnemyAbilityData usedAbility, IReadOnlyList<EnemyAbilityData> allAbilities)
    {
        if (usedAbility == null || allAbilities == null)
            return;

        foreach (EnemyAbilityData ability in allAbilities)
        {
            if (ability == null)
                continue;

            EnemyAbilityState state = GetState(ability);
            if (ability == usedAbility)
            {
                state.Uses++;
                state.CooldownRemaining = Mathf.Max(0, ability.cooldownTurns);
            }
            else if (state.CooldownRemaining > 0)
            {
                state.CooldownRemaining--;
            }
        }
    }

    private EnemyAbilityState GetState(EnemyAbilityData ability)
    {
        if (!states.TryGetValue(ability, out EnemyAbilityState state))
        {
            state = new EnemyAbilityState();
            states[ability] = state;
        }

        return state;
    }
}
