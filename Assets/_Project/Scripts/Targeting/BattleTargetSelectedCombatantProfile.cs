using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Selected Combatant", fileName = "Target_SelectedCombatant")]
public class BattleTargetSelectedCombatantProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx)
    {
        return ctx.SelectedTarget != null
            ? new[] { ctx.SelectedTarget }
            : System.Array.Empty<ICombatant>();
    }
}
