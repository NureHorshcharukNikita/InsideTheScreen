using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/All Enemies", fileName = "Target_AllEnemies")]
public class BattleTargetAllEnemiesProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx)
    {
        if (ctx.Enemies == null || ctx.Enemies.Count == 0)
            return System.Array.Empty<ICombatant>();

        var arr = new ICombatant[ctx.Enemies.Count];
        for (int i = 0; i < ctx.Enemies.Count; i++)
            arr[i] = ctx.Enemies[i];

        return arr;
    }
}
