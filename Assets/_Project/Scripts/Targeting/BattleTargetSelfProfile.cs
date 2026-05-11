using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Self", fileName = "Target_Self")]
public class BattleTargetSelfProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx)
    {
        return ctx.Self != null ? new[] { ctx.Self } : System.Array.Empty<ICombatant>();
    }
}
