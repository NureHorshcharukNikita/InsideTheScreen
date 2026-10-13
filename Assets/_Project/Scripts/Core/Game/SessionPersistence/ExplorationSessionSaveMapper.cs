using UnityEngine;

public static class ExplorationSessionSaveMapper
{
    public static SessionSaveData Create(
        bool hasSession,
        string continueSceneName,
        bool hasSavedPosition,
        Vector3 savedPosition,
        bool hasSavedHealth,
        int savedHealth,
        bool hasGold,
        int gold,
        InventoryData runtimeInventory,
        DeckData runtimeDeck,
        QuestProgressSaveData[] questProgress,
        ShopProgressSaveData[] shopProgress,
        NpcPositionSaveData[] npcPositions,
        NpcDialogueProgressSaveData[] npcDialogueProgress,
        bool hasSkillProgression,
        int skillPoints,
        SkillProgressSaveData[] skillProgression,
        string[] storyFlags)
    {
        SessionSaveData data = new()
        {
            hasSession = hasSession,
            continueSceneName = continueSceneName,
            hasSavedPosition = hasSavedPosition,
            positionX = savedPosition.x,
            positionY = savedPosition.y,
            hasSavedHealth = hasSavedHealth,
            savedHealth = savedHealth,
            hasGold = hasGold,
            gold = gold,
            pendingEncounterId = PendingBattleEnemy.EncounterId,
            inventoryCardIds = SessionCardSaveMapper.ToIds(runtimeInventory?.Cards),
            deckCardIds = SessionCardSaveMapper.ToIds(runtimeDeck?.Cards),
            defeatedEncounterIds = DefeatedEncounters.ToArray(),
            encounterGroups = EncounterGroups.ToArray(),
            collectedPickupIds = CollectedPickups.ToArray(),
            questProgress = questProgress,
            shopProgress = shopProgress,
            npcPositions = npcPositions,
            npcDialogueProgress = npcDialogueProgress,
            hasSkillProgression = hasSkillProgression,
            skillPoints = skillPoints,
            skillProgression = skillProgression,
            storyFlags = storyFlags
        };

        int[] enemyHealths = PendingBattleEnemy.CurrentEnemyHealths;
        if (enemyHealths != null && enemyHealths.Length > 0)
        {
            data.enemyHealths = enemyHealths;
            data.hasEnemyHealth = true;
            data.enemyHealth = enemyHealths[0];
        }

        return data;
    }
}
