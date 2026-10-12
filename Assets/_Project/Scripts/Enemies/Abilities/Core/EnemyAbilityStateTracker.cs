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

    // Whether the ability can go into a turn plan that already holds the given abilities:
    // one with a cooldown is used at most once per turn, and planned uses count towards Max Uses.
    public bool CanPlanAgain(EnemyAbilityData ability, IReadOnlyList<EnemyAbilityData> plannedThisTurn)
    {
        int timesPlanned = CountOf(ability, plannedThisTurn);
        if (timesPlanned == 0)
            return true;

        if (ability.cooldownTurns > 0)
            return false;

        return ability.maxUses < 0 || GetState(ability).Uses + timesPlanned < ability.maxUses;
    }

    // Cooldowns count turns, not actions: a turn with two abilities still ticks them down once.
    public void RecordTurn(IReadOnlyList<EnemyAbilityData> usedThisTurn, IReadOnlyList<EnemyAbilityData> allAbilities)
    {
        if (allAbilities == null)
            return;

        foreach (EnemyAbilityData ability in allAbilities)
        {
            if (ability == null)
                continue;

            EnemyAbilityState state = GetState(ability);
            int timesUsed = CountOf(ability, usedThisTurn);
            if (timesUsed > 0)
            {
                state.Uses += timesUsed;
                state.CooldownRemaining = Mathf.Max(0, ability.cooldownTurns);
            }
            else if (state.CooldownRemaining > 0)
            {
                state.CooldownRemaining--;
            }
        }
    }

    private static int CountOf(EnemyAbilityData ability, IReadOnlyList<EnemyAbilityData> abilities)
    {
        if (abilities == null)
            return 0;

        int count = 0;
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] == ability)
                count++;
        }

        return count;
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
