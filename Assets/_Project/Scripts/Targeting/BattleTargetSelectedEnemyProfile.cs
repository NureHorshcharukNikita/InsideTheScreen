using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Selected Enemy", fileName = "Target_SelectedEnemy")]
public class BattleTargetSelectedEnemyProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        return IsOnTeam(context.SelectedTarget, context.Enemies)
            ? new[] { context.SelectedTarget }
            : System.Array.Empty<ICombatant>();
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
