using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Self", fileName = "Target_Self")]
public class BattleTargetSelfProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        return context.Self != null ? new[] { context.Self } : System.Array.Empty<ICombatant>();
    }

    public override bool CanUseWithContext(BattleTargetingContext context)
    {
        return context.SelectedTarget == null || ReferenceEquals(context.SelectedTarget, context.Self);
    }
}
