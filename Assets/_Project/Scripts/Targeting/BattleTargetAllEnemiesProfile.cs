using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/All Enemies", fileName = "Target_AllEnemies")]
public class BattleTargetAllEnemiesProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        if (context.Enemies == null || context.Enemies.Count == 0)
            return System.Array.Empty<ICombatant>();

        var targets = new ICombatant[context.Enemies.Count];
        for (int i = 0; i < context.Enemies.Count; i++)
            targets[i] = context.Enemies[i];

        return targets;
    }
}
