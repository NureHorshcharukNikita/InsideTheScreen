using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Single Enemy", fileName = "Target_SingleEnemy")]
public class BattleTargetSingleEnemyProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx)
    {
        if (ctx.Enemies == null || ctx.Enemies.Count == 0)
            return System.Array.Empty<ICombatant>();

        int i = Random.Range(0, ctx.Enemies.Count);
        return new[] { ctx.Enemies[i] };
    }
}
