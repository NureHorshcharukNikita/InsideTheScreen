using System.Collections.Generic;
using UnityEngine;

public abstract class BattleCondition : ScriptableObject
{
    [Header("Condition Info")]
    [SerializeField] private string conditionID;
    [SerializeField] private string displayName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    public string ConditionID => conditionID;
    public string DisplayName => displayName;
    public string Description => description;

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
