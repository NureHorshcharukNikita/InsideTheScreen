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

    [Tooltip("Give Cards: the card put into the inventory, e.g. berries picked from a bush.")]
    public CardData card;
    [Tooltip("Give Cards: how many.")]
    [Min(1)] public int count = 1;

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
            case DialogueActionType.GiveCards:
                GiveCards(player);
                break;
        }
    }

    private void GiveCards(PlayerCharacter player)
    {
        player ??= UnityEngine.Object.FindFirstObjectByType<PlayerCharacter>();
        InventoryData inventory = player != null ? player.InventoryData : ExplorationPlayerSession.RuntimeInventory;
        if (card == null || inventory == null)
            return;

        inventory.AddCards(card, Mathf.Max(1, count));
        ExplorationPlayerSession.AddToDeckIfRoom(card, Mathf.Max(1, count));
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
    StartBattle,
    GiveCards
}
