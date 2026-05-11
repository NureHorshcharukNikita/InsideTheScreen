using System.Collections.Generic;

public static partial class CardResolver
{
    private static bool SelectionMatchesProfile(BattleTargetingProfile profile, BattleTargetingContext ctx)
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
                return IsOnTeam(ctx.SelectedTarget, ctx.Enemies);
            case BattleTargetSelectedAllyProfile:
                return IsOnTeam(ctx.SelectedTarget, ctx.Allies);
            case BattleTargetSelectedCombatantProfile:
                return ReferenceEquals(ctx.SelectedTarget, ctx.Self)
                       || IsOnTeam(ctx.SelectedTarget, ctx.Allies)
                       || IsOnTeam(ctx.SelectedTarget, ctx.Enemies);
            case BattleTargetSelectedOtherCombatantProfile:
                return !ReferenceEquals(ctx.SelectedTarget, ctx.Self)
                       && (IsOnTeam(ctx.SelectedTarget, ctx.Allies) || IsOnTeam(ctx.SelectedTarget, ctx.Enemies));
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
