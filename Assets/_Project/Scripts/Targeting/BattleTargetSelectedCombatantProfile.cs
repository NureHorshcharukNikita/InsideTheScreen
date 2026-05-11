using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Selected Combatant", fileName = "Target_SelectedCombatant")]
public class BattleTargetSelectedCombatantProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        return CanUseWithContext(context)
            ? new[] { context.SelectedTarget }
            : System.Array.Empty<ICombatant>();
    }

    public override bool CanUseWithContext(BattleTargetingContext context)
    {
        return ReferenceEquals(context.SelectedTarget, context.Self)
               || BattleTargetingTeams.Contains(context.SelectedTarget, context.Allies)
               || BattleTargetingTeams.Contains(context.SelectedTarget, context.Enemies);
    }
}
