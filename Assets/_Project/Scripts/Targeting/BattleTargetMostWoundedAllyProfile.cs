using System.Collections.Generic;
using UnityEngine;

// The living ally, the source included, with the lowest share of health left; on a tie the first
// one, so Hideshi tends Haru before himself. Unlike Random Ally, the intent shows the fighter
// who really gets the heal or armor.
[CreateAssetMenu(menuName = "Game/Battle/Targeting/Most Wounded Ally", fileName = "Target_MostWoundedAlly")]
public class BattleTargetMostWoundedAllyProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        ICombatant mostWounded = null;
        float lowestRatio = float.MaxValue;
        foreach (ICombatant ally in context.Allies)
        {
            if (ally == null || ally.MaxHealth <= 0)
                continue;

            float ratio = (float)ally.CurrentHealth / ally.MaxHealth;
            if (ratio < lowestRatio)
            {
                mostWounded = ally;
                lowestRatio = ratio;
            }
        }

        return mostWounded != null
            ? new[] { mostWounded }
            : System.Array.Empty<ICombatant>();
    }
}
