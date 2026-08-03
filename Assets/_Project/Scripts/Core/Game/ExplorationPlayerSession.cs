using UnityEngine;

public static class ExplorationPlayerSession
{
    private static Vector3 savedPosition;
    private static bool hasSavedPosition;
    private static int savedHealth;
    private static bool hasSavedHealth;
    private static InventoryData runtimeInventory;
    private static DeckData runtimeDeck;
    private static bool persistentLoaded;
    private static SessionSaveData loadedPersistentData = null;

    public static bool HasSession { get; private set; }
    public static string ContinueSceneName { get; private set; } = SceneNames.Exploration;

    public static void SavePosition(Vector3 position)
    {
        savedPosition = position;
        hasSavedPosition = true;
        HasSession = true;
        SavePersistent();
    }

    public static bool TryGetSavedPosition(out Vector3 position)
    {
        position = savedPosition;
        return hasSavedPosition;
    }

    public static void SaveHealth(int health)
    {
        savedHealth = health;
        hasSavedHealth = true;
        HasSession = true;
        SavePersistent();
    }

    public static bool TryGetSavedHealth(out int health)
    {
        health = savedHealth;
        return hasSavedHealth;
    }

    public static InventoryData GetOrCreateRuntimeInventory(InventoryData template)
    {
        if (template == null)
            return null;

        if (runtimeInventory == null)
        {
            runtimeInventory = template.CreateRuntimeCopy();
            RestoreRuntimeInventoryFromLoadedData(template);
        }

        HasSession = true;
        SavePersistent();
        return runtimeInventory;
    }

    public static DeckData GetOrCreateRuntimeDeck(DeckData template)
    {
        if (template == null)
            return null;

        if (runtimeDeck == null)
        {
            runtimeDeck = template.CreateRuntimeCopy();
            RestoreRuntimeDeckFromLoadedData(template);
        }

        HasSession = true;
        SavePersistent();
        return runtimeDeck;
    }

    public static void SavePlayer(PlayerCharacter player)
    {
        if (player == null)
            return;

        SavePosition(player.transform.position);
        SaveHealth(player.CurrentHealth);
        ContinueSceneName = SceneNames.Exploration;
    }

    public static void SaveBattle(PlayerCharacter player)
    {
        if (player != null)
            SaveHealth(player.CurrentHealth);

        HasSession = true;
        ContinueSceneName = SceneNames.Battle;
        SavePersistent();
    }

    public static void SaveBattle(PlayerCharacter player, EnemyCharacter enemy)
    {
        SaveBattle(player);

        if (enemy != null)
            PendingBattleEnemy.SaveCurrentEnemyHealth(enemy.CurrentHealth);

        SavePersistent();
    }

    public static void SetContinueScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        HasSession = true;
        ContinueSceneName = sceneName;
        SavePersistent();
    }

    public static void LoadPersistentSaveIfNeeded()
    {
#if !UNITY_EDITOR
        if (persistentLoaded)
            return;

        persistentLoaded = true;

        if (!PersistentSessionSave.TryLoad(out SessionSaveData data))
            return;

        loadedPersistentData = data;
        RestoreFrom(data);
#endif
    }

    public static void SavePersistent()
    {
#if !UNITY_EDITOR
        if (!HasSession)
            return;

        PersistentSessionSave.Save(CreateSaveData());
#endif
    }

    public static void Clear()
    {
        savedPosition = default;
        hasSavedPosition = false;
        savedHealth = default;
        hasSavedHealth = false;
        runtimeInventory = null;
        runtimeDeck = null;
        HasSession = false;
        ContinueSceneName = SceneNames.Exploration;
#if !UNITY_EDITOR
        PersistentSessionSave.Delete();
#endif
    }

    private static SessionSaveData CreateSaveData()
    {
        return ExplorationSessionSaveMapper.Create(
            HasSession,
            ContinueSceneName,
            hasSavedPosition,
            savedPosition,
            hasSavedHealth,
            savedHealth,
            runtimeInventory,
            runtimeDeck);
    }

    private static void RestoreFrom(SessionSaveData data)
    {
        HasSession = data.hasSession;
        ContinueSceneName = string.IsNullOrEmpty(data.continueSceneName)
            ? SceneNames.Exploration
            : data.continueSceneName;

        hasSavedPosition = data.hasSavedPosition;
        savedPosition = new Vector3(data.positionX, data.positionY, 0f);

        hasSavedHealth = data.hasSavedHealth;
        savedHealth = data.savedHealth;

        PendingBattleEnemy.RestoreSession(
            data.pendingEncounterId,
            data.hasEnemyHealth,
            data.enemyHealth);

        DefeatedEncounters.Restore(data.defeatedEncounterIds);
    }

    private static void RestoreRuntimeInventoryFromLoadedData(InventoryData template)
    {
        if (loadedPersistentData?.inventoryCardIds == null || runtimeInventory == null)
            return;

        var cardsById = SessionCardSaveMapper.BuildLookup(template, runtimeDeck);
        runtimeInventory.ReplaceCards(SessionCardSaveMapper.ResolveCards(loadedPersistentData.inventoryCardIds, cardsById));
    }

    private static void RestoreRuntimeDeckFromLoadedData(DeckData template)
    {
        if (loadedPersistentData?.deckCardIds == null || runtimeDeck == null)
            return;

        var cardsById = SessionCardSaveMapper.BuildLookup(runtimeInventory, template);
        runtimeDeck.ReplaceCards(SessionCardSaveMapper.ResolveCards(loadedPersistentData.deckCardIds, cardsById));
    }
}
