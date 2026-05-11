using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Random Enemy", fileName = "Target_RandomEnemy")]
public class BattleTargetRandomEnemyProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        if (context.Enemies == null || context.Enemies.Count == 0)
            return System.Array.Empty<ICombatant>();

        int randomIndex = Random.Range(0, context.Enemies.Count);
        return new[] { context.Enemies[randomIndex] };
    }
}
