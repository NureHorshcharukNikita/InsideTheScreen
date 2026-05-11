using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Random Ally", fileName = "Target_RandomAlly")]
public class BattleTargetRandomAllyProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        if (context.Allies == null || context.Allies.Count == 0)
            return System.Array.Empty<ICombatant>();

        int randomIndex = Random.Range(0, context.Allies.Count);
        return new[] { context.Allies[randomIndex] };
    }
}
