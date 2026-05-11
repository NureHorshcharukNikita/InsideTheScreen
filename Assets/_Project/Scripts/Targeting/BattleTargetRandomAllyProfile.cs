using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Random Ally", fileName = "Target_RandomAlly")]
public class BattleTargetRandomAllyProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx)
    {
        if (ctx.Allies == null || ctx.Allies.Count == 0)
            return System.Array.Empty<ICombatant>();

        int i = Random.Range(0, ctx.Allies.Count);
        return new[] { ctx.Allies[i] };
    }
}
