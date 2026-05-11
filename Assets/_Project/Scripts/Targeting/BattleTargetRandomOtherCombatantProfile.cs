using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Targeting/Random Other Combatant", fileName = "Target_RandomOtherCombatant")]
public class BattleTargetRandomOtherCombatantProfile : BattleTargetingProfile
{
    public override IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext context)
    {
        var candidates = new List<ICombatant>();
        AddAllExceptSelf(candidates, context.Allies, context.Self);
        AddAllExceptSelf(candidates, context.Enemies, context.Self);

        if (candidates.Count == 0)
            return System.Array.Empty<ICombatant>();

        int index = Random.Range(0, candidates.Count);
        return new[] { candidates[index] };
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
