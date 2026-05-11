using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Random Combatant", fileName = "Target_RandomCombatant")]
public class BattleTargetRandomCombatantProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx)
    {
        int alliesCount = ctx.Allies?.Count ?? 0;
        int enemiesCount = ctx.Enemies?.Count ?? 0;
        int totalCount = alliesCount + enemiesCount;

        if (totalCount == 0)
            return System.Array.Empty<ICombatant>();

        int index = Random.Range(0, totalCount);
        if (index < alliesCount)
            return new[] { ctx.Allies[index] };

        return new[] { ctx.Enemies[index - alliesCount] };
    }
}
