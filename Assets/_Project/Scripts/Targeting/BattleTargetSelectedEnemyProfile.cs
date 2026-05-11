using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Selected Enemy", fileName = "Target_SelectedEnemy")]
public class BattleTargetSelectedEnemyProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        return CanUseWithContext(context)
            ? new[] { context.SelectedTarget }
            : System.Array.Empty<ICombatant>();
    }

    public override bool CanUseWithContext(BattleTargetingContext context)
    {
        return BattleTargetingTeams.Contains(context.SelectedTarget, context.Enemies);
    }
}
