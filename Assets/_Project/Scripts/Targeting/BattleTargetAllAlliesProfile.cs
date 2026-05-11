using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/All Allies", fileName = "Target_AllAllies")]
public class BattleTargetAllAlliesProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx)
    {
        if (ctx.Allies == null || ctx.Allies.Count == 0)
            return System.Array.Empty<ICombatant>();

        var arr = new ICombatant[ctx.Allies.Count];
        for (int i = 0; i < ctx.Allies.Count; i++)
            arr[i] = ctx.Allies[i];

        return arr;
    }
}
