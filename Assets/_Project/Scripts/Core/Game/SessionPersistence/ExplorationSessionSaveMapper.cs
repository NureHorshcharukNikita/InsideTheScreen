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
        InventoryData runtimeInventory,
        DeckData runtimeDeck)
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
            pendingEncounterId = PendingBattleEnemy.EncounterId,
            inventoryCardIds = SessionCardSaveMapper.ToIds(runtimeInventory?.Cards),
            deckCardIds = SessionCardSaveMapper.ToIds(runtimeDeck?.Cards),
            defeatedEncounterIds = DefeatedEncounters.ToArray()
        };

        if (PendingBattleEnemy.TryGetCurrentEnemyHealth(out int enemyHealth))
        {
            data.hasEnemyHealth = true;
            data.enemyHealth = enemyHealth;
        }

        return data;
    }
}
