using System.Collections.Generic;

public static partial class CardResolver
{
    private static bool SelectionMatchesProfile(BattleTargetingProfile profile, BattleTargetingContext context)
    {
        switch (profile)
        {
            case BattleTargetSelfProfile:
            case BattleTargetRandomEnemyProfile:
            case BattleTargetAllEnemiesProfile:
            case BattleTargetRandomAllyProfile:
            case BattleTargetAllAlliesProfile:
            case BattleTargetRandomCombatantProfile:
            case BattleTargetRandomOtherCombatantProfile:
            case BattleTargetAllCombatantsProfile:
            case BattleTargetAllOtherCombatantsProfile:
                return true;
            case BattleTargetSelectedEnemyProfile:
                return IsOnTeam(context.SelectedTarget, context.Enemies);
            case BattleTargetSelectedAllyProfile:
                return IsOnTeam(context.SelectedTarget, context.Allies);
            case BattleTargetSelectedCombatantProfile:
                return ReferenceEquals(context.SelectedTarget, context.Self)
                       || IsOnTeam(context.SelectedTarget, context.Allies)
                       || IsOnTeam(context.SelectedTarget, context.Enemies);
            case BattleTargetSelectedOtherCombatantProfile:
                return !ReferenceEquals(context.SelectedTarget, context.Self)
                       && (IsOnTeam(context.SelectedTarget, context.Allies) || IsOnTeam(context.SelectedTarget, context.Enemies));
            default:
                return true;
        }
    }

    private static bool IsOnTeam(ICombatant unit, IReadOnlyList<ICombatant> team)
    {
        if (unit == null || team == null)
            return false;

        for (int i = 0; i < team.Count; i++)
        {
            if (ReferenceEquals(team[i], unit))
                return true;
        }

        return false;
    }
}
