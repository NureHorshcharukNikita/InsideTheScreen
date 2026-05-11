using System.Collections.Generic;

public static partial class CardResolver
{
    private static bool SelectionMatchesProfile(BattleTargetingProfile profile, BattleTargetingContext ctx)
    {
        switch (profile)
        {
            case BattleTargetSelfProfile:
                return ReferenceEquals(ctx.SelectedTarget, ctx.Self);
            case BattleTargetSingleEnemyProfile:
            case BattleTargetAllEnemiesProfile:
            case BattleTargetSelectedProfile:
                return IsOnTeam(ctx.SelectedTarget, ctx.Enemies);
            case BattleTargetSingleAllyProfile:
            case BattleTargetAllAlliesProfile:
                return IsOnTeam(ctx.SelectedTarget, ctx.Allies);
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
