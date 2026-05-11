using System.Collections.Generic;

public partial class EnemyBrain
{
    private sealed class AbilityRuntimeState
    {
        public int CooldownRemaining;
        public int Uses;
    }

    private static AbilityRuntimeState GetState(Dictionary<EnemyAbilityData, AbilityRuntimeState> runtime, EnemyAbilityData ability)
    {
        if (!runtime.TryGetValue(ability, out AbilityRuntimeState state))
        {
            state = new AbilityRuntimeState();
            runtime[ability] = state;
        }

        return state;
    }

    private static bool IsAbilityAvailable(
        EnemyAbilityData ability,
        BattleTargetingContext ctx,
        Dictionary<EnemyAbilityData, AbilityRuntimeState> runtime)
    {
        AbilityRuntimeState state = GetState(runtime, ability);
        if (state.CooldownRemaining > 0)
            return false;

        if (ability.maxUses >= 0 && state.Uses >= ability.maxUses)
            return false;

        if (!BattleCondition.AllMet(ability.conditions, ctx))
            return false;

        return true;
    }
}
