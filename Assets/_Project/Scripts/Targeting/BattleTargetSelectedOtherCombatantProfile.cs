using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Selected Other Combatant", fileName = "Target_SelectedOtherCombatant")]
public class BattleTargetSelectedOtherCombatantProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        return CanUseWithContext(context)
            ? new[] { context.SelectedTarget }
            : System.Array.Empty<ICombatant>();
    }

    public override bool CanUseWithContext(BattleTargetingContext context)
    {
        return !ReferenceEquals(context.SelectedTarget, context.Self)
               && (BattleTargetingTeams.Contains(context.SelectedTarget, context.Allies)
                   || BattleTargetingTeams.Contains(context.SelectedTarget, context.Enemies));
    }
}
