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

    public abstract bool IsMet(BattleTargetingContext context);

    public static bool AllMet(IReadOnlyList<BattleCondition> conditions, BattleTargetingContext context)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            BattleCondition condition = conditions[i];
            if (condition == null)
                continue;

            if (!condition.IsMet(context))
                return false;
        }

        return true;
    }
}
