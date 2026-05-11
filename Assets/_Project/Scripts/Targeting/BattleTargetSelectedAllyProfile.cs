using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Selected Ally", fileName = "Target_SelectedAlly")]
public class BattleTargetSelectedAllyProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        return CanUseWithContext(context)
            ? new[] { context.SelectedTarget }
            : System.Array.Empty<ICombatant>();
    }

    public override bool CanUseWithContext(BattleTargetingContext context)
    {
        return BattleTargetingTeams.Contains(context.SelectedTarget, context.Allies);
    }
}
