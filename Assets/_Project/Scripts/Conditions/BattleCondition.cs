using System.Collections.Generic;
using UnityEngine;

public abstract class BattleCondition : ScriptableObject
{
    public abstract bool IsMet(BattleTargetingContext ctx);

    public static bool AllMet(IReadOnlyList<BattleCondition> conditions, BattleTargetingContext ctx)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            BattleCondition c = conditions[i];
            if (c == null)
                continue;

            if (!c.IsMet(ctx))
                return false;
        }

        return true;
    }
}
