using System;
using UnityEngine;

[Serializable]
public sealed class NpcDialogueEntry
{
    [Tooltip("Optional save key. If empty, the dialogue asset name is used.")]
    public string key;
    public DialogueData dialogue;
    public bool repeatable = true;

    [Header("Talk Timing")]
    public NpcDialogueTalkTiming talkTiming = NpcDialogueTalkTiming.AnyTime;

    [Tooltip("1 means the first conversation, 2 means the second conversation, and so on.")]
    public int conversationNumber = 1;

    [Tooltip("1 means the first conversation.")]
    public int rangeStartConversation = 1;

    [Tooltip("0 means no upper limit.")]
    public int rangeEndConversation;

    [Header("Quest Condition")]
    public bool requireQuestStatus;
    public QuestData quest;
    public QuestStatus questStatus = QuestStatus.Active;

    [Header("Story Flag Condition")]
    public string requiredStoryFlag;
    public bool invertStoryFlag;

    public string Key
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(key))
                return key;

            return dialogue != null ? dialogue.name : string.Empty;
        }
    }

    public bool HasDialogue => dialogue != null && dialogue.HasLines;

    public bool IsAvailable(string npcKey)
    {
        if (!HasDialogue)
            return false;

        if (!IsTalkTimingAvailable(npcKey))
            return false;

        if (!repeatable && NpcDialogueProgress.WasDialogueUsed(npcKey, Key))
            return false;

        if (requireQuestStatus)
        {
            if (quest == null || QuestManager.GetStatus(quest) != questStatus)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(requiredStoryFlag))
        {
            bool hasFlag = StoryFlagManager.HasFlag(requiredStoryFlag);
            if (invertStoryFlag ? hasFlag : !hasFlag)
                return false;
        }

        return true;
    }

    private bool IsTalkTimingAvailable(string npcKey)
    {
        int conversationNumberNow = NpcDialogueProgress.GetTalkCount(npcKey) + 1;

        return talkTiming switch
        {
            NpcDialogueTalkTiming.FirstConversation => conversationNumberNow == 1,
            NpcDialogueTalkTiming.AfterFirstConversation => conversationNumberNow > 1,
            NpcDialogueTalkTiming.ConversationNumber => conversationNumberNow == Mathf.Max(1, conversationNumber),
            NpcDialogueTalkTiming.ConversationRange => IsConversationInRange(conversationNumberNow),
            _ => true
        };
    }

    private bool IsConversationInRange(int conversationNumberNow)
    {
        int start = Mathf.Max(1, rangeStartConversation);
        int end = rangeEndConversation;
        return conversationNumberNow >= start && (end <= 0 || conversationNumberNow <= end);
    }
}

public enum NpcDialogueTalkTiming
{
    AnyTime,
    FirstConversation,
    AfterFirstConversation,
    ConversationNumber,
    ConversationRange
}
