using System;
using UnityEngine;

[Serializable]
public sealed class DialogueAction
{
    public DialogueActionType type = DialogueActionType.None;
    public QuestData quest;
    public string storyFlag;
    public bool silent;
    public ShopData shop;

    public void Execute(NpcCharacter npc = null, PlayerCharacter player = null)
    {
        switch (type)
        {
            case DialogueActionType.StartQuest:
                QuestManager.StartQuest(quest, !silent);
                break;
            case DialogueActionType.CompleteQuest:
                player ??= UnityEngine.Object.FindFirstObjectByType<PlayerCharacter>();
                QuestManager.CompleteQuest(quest, player != null ? player.InventoryData : null, !silent);
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
            case DialogueActionType.OpenShop:
                OpenShop(npc, player);
                break;
        }
    }

    private void OpenShop(NpcCharacter npc, PlayerCharacter player)
    {
        ShopData shopToOpen = shop != null ? shop : npc != null ? npc.Shop : null;
        if (shopToOpen == null)
            return;

        DialogueManager.Instance?.EndDialogue();

        player ??= UnityEngine.Object.FindFirstObjectByType<PlayerCharacter>(UnityEngine.FindObjectsInactive.Include);
        ShopWindow.Open(shopToOpen, npc, player);
    }
}

public enum DialogueActionType
{
    None,
    StartQuest,
    CompleteQuest,
    FailQuest,
    SetStoryFlag,
    ClearStoryFlag,
    OpenShop
}
