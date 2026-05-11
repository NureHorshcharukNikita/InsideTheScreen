using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Selected Combatant", fileName = "Target_SelectedCombatant")]
public class BattleTargetSelectedCombatantProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        return context.SelectedTarget != null
            ? new[] { context.SelectedTarget }
            : System.Array.Empty<ICombatant>();
    }
}
