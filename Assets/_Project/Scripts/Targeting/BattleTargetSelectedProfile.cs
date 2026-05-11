using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Selected Target", fileName = "Target_Selected")]
public class BattleTargetSelectedProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx)
    {
        return ctx.SelectedTarget != null ? new[] { ctx.SelectedTarget } : System.Array.Empty<ICombatant>();
    }
}
