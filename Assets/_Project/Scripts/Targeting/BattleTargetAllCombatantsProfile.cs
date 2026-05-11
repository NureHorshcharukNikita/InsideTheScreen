using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/All Combatants", fileName = "Target_AllCombatants")]
public class BattleTargetAllCombatantsProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        int alliesCount = context.Allies?.Count ?? 0;
        int enemiesCount = context.Enemies?.Count ?? 0;

        if (alliesCount == 0 && enemiesCount == 0)
            return System.Array.Empty<ICombatant>();

        var targets = new List<ICombatant>(alliesCount + enemiesCount);
        AddAll(targets, context.Allies);
        AddAll(targets, context.Enemies);
        return targets;
    }

    private static void AddAll(List<ICombatant> targets, IReadOnlyList<ICombatant> source)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
                targets.Add(source[i]);
        }
    }
}
