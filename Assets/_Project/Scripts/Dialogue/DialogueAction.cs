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

    [Tooltip("Start Battle: the enemy to fight once the dialogue closes.")]
    public EnemyData enemy;
    [Tooltip("Start Battle: remembers that this fight was won. Use a unique id.")]
    public string encounterId;

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
            case DialogueActionType.StartBattle:
                StartBattle();
                break;
        }
    }

    // Like walking into an enemy on the map: the player and the NPCs come back to where they stood.
    private void StartBattle()
    {
        if (enemy == null)
        {
            Debug.LogWarning("Start Battle dialogue action has no enemy.");
            return;
        }

        PlayerMovement movement = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        if (movement != null)
        {
            ExplorationPlayerSession.SavePosition(movement.transform.position);
            movement.StopMovement();
            movement.enabled = false;
        }

        ExplorationPlayerSession.SaveNpcPositions();

        PendingBattleEnemy.RegisterEncounterStart(
            new[] { enemy },
            CompanionFollower.CollectBattleAllies(),
            string.IsNullOrWhiteSpace(encounterId) ? enemy.name : encounterId);
        FadeManager.TryFadeToScene(SceneNames.Battle);
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
    OpenShop,
    StartBattle
}
