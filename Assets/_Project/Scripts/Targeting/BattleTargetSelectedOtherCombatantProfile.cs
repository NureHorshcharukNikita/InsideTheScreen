using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Selected Other Combatant", fileName = "Target_SelectedOtherCombatant")]
public class BattleTargetSelectedOtherCombatantProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx)
    {
        if (ctx.SelectedTarget == null || ReferenceEquals(ctx.SelectedTarget, ctx.Self))
            return System.Array.Empty<ICombatant>();

        return new[] { ctx.SelectedTarget };
    }
}
