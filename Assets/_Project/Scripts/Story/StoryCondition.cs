using System;
using System.Collections.Generic;
using UnityEngine;

public enum StoryConditionType
{
    StoryFlag,
    QuestStatus,
    QuestObjectiveDone
}

// One requirement of an NPC dialogue entry or a story event: a story flag is set,
// or a quest is in a given status, or a Defeat Enemies / Collect Cards quest is active and has
// reached its count (the player can hand it in). Invert turns it around (the flag is not set, the
// quest is not in that status). A list of conditions is met when all of them are; an empty list always is.
[Serializable]
public sealed class StoryCondition
{
    public StoryConditionType type;
    [StoryFlag] public string storyFlag;
    public QuestData quest;
    public QuestStatus questStatus = QuestStatus.Active;
    [Tooltip("Met when the opposite is true: the flag is not set, the quest is not in this status.")]
    public bool invert;

    // A condition with no flag or quest picked yet is ignored.
    public bool IsMet()
    {
        if (type == StoryConditionType.QuestStatus)
            return quest == null || (QuestManager.GetStatus(quest) == questStatus) != invert;

        if (type == StoryConditionType.QuestObjectiveDone)
            return quest == null || QuestManager.IsObjectiveDone(quest) != invert;

        return string.IsNullOrWhiteSpace(storyFlag) || StoryFlagManager.HasFlag(storyFlag) != invert;
    }

    public static bool AreMet(IReadOnlyList<StoryCondition> conditions)
    {
        if (conditions == null)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            if (conditions[i] != null && !conditions[i].IsMet())
                return false;
        }

        return true;
    }
}
