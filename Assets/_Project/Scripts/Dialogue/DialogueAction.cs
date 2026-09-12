using System;

[Serializable]
public sealed class DialogueAction
{
    public DialogueActionType type = DialogueActionType.None;
    public QuestData quest;
    public string storyFlag;
    public bool silent;

    public void Execute()
    {
        switch (type)
        {
            case DialogueActionType.StartQuest:
                QuestManager.StartQuest(quest, !silent);
                break;
            case DialogueActionType.CompleteQuest:
                QuestManager.SetStatus(quest, QuestStatus.Completed);
                break;
            case DialogueActionType.FailQuest:
                QuestManager.SetStatus(quest, QuestStatus.Failed);
                break;
            case DialogueActionType.SetStoryFlag:
                StoryFlagManager.SetFlag(storyFlag);
                break;
            case DialogueActionType.ClearStoryFlag:
                StoryFlagManager.ClearFlag(storyFlag);
                break;
        }
    }
}

public enum DialogueActionType
{
    None,
    StartQuest,
    CompleteQuest,
    FailQuest,
    SetStoryFlag,
    ClearStoryFlag
}
