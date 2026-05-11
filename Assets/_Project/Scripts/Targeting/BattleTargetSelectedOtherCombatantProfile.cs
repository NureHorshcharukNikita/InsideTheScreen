using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Selected Other Combatant", fileName = "Target_SelectedOtherCombatant")]
public class BattleTargetSelectedOtherCombatantProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        if (context.SelectedTarget == null || ReferenceEquals(context.SelectedTarget, context.Self))
            return System.Array.Empty<ICombatant>();

        return new[] { context.SelectedTarget };
    }
}
