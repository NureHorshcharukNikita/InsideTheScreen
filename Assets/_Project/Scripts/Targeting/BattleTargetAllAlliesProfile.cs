using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/All Allies", fileName = "Target_AllAllies")]
public class BattleTargetAllAlliesProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        if (context.Allies == null || context.Allies.Count == 0)
            return System.Array.Empty<ICombatant>();

        var targets = new ICombatant[context.Allies.Count];
        for (int i = 0; i < context.Allies.Count; i++)
            targets[i] = context.Allies[i];

        return targets;
    }
}
