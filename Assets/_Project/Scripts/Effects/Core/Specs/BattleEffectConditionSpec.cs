using System;
using System.Collections.Generic;

[Serializable]
public class BattleEffectConditionSpec
{
    public BattleCondition condition;

    public bool IsMet(BattleTargetingContext context)
    {
        return condition != null && condition.IsMet(context);
    }

    public static bool AllMet(IReadOnlyList<BattleEffectConditionSpec> conditions, BattleTargetingContext context)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            BattleEffectConditionSpec conditionSpec = conditions[i];
            if (conditionSpec == null)
                continue;

            if (!conditionSpec.IsMet(context))
                return false;
        }

        return true;
    }
}
