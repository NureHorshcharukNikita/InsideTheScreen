using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/All Other Combatants", fileName = "Target_AllOtherCombatants")]
public class BattleTargetAllOtherCombatantsProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx)
    {
        int alliesCount = ctx.Allies?.Count ?? 0;
        int enemiesCount = ctx.Enemies?.Count ?? 0;

        if (alliesCount == 0 && enemiesCount == 0)
            return System.Array.Empty<ICombatant>();

        var targets = new List<ICombatant>(alliesCount + enemiesCount);
        AddAllExceptSelf(targets, ctx.Allies, ctx.Self);
        AddAllExceptSelf(targets, ctx.Enemies, ctx.Self);
        return targets;
    }

    private static void AddAllExceptSelf(List<ICombatant> targets, IReadOnlyList<ICombatant> source, ICombatant self)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            ICombatant target = source[i];
            if (target == null || ReferenceEquals(target, self))
                continue;

            targets.Add(target);
        }
    }
}
