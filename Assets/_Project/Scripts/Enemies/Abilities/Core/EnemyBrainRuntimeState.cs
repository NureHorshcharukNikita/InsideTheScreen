using System.Collections.Generic;

internal static class EnemyBrainRuntime
{
    internal sealed class AbilityRuntimeState
    {
        public int CooldownRemaining;
        public int Uses;
    }

    public static AbilityRuntimeState GetState(Dictionary<EnemyAbilityData, AbilityRuntimeState> runtime, EnemyAbilityData ability)
    {
        if (!runtime.TryGetValue(ability, out AbilityRuntimeState state))
        {
            state = new AbilityRuntimeState();
            runtime[ability] = state;
        }

        return state;
    }

    public static bool IsAbilityAvailable(
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
