using System.Collections.Generic;
using UnityEngine;

public static class ExplorationPlayerSession
{
    private static Vector3 savedPosition;
    private static bool hasSavedPosition;
    private static int savedHealth;
    private static bool hasSavedHealth;
    private static InventoryData runtimeInventory;
    private static DeckData runtimeDeck;
    private static readonly Dictionary<string, Vector3> npcPositions = new();
    private static bool persistentLoaded;
    private static bool initialSavePending;
    private static SessionSaveData loadedPersistentData = null;

    public static bool HasSession { get; private set; }
    public static string ContinueSceneName { get; private set; } = SceneNames.Exploration;
    public static bool HasAnyPersistentSession => PersistentSessionSave.HasAnySlot();

    public static void SavePosition(Vector3 position)
    {
        savedPosition = position;
        hasSavedPosition = true;
        HasSession = true;
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
        return runtimeDeck;
    }

    public static void SavePlayer(PlayerCharacter player)
    {
        if (player == null)
            return;

        savedPosition = player.transform.position;
        hasSavedPosition = true;
        savedHealth = player.CurrentHealth;
        hasSavedHealth = true;
        HasSession = true;
        ContinueSceneName = SceneNames.Exploration;
        PersistCurrentSession();
        PersistentSessionSave.CaptureActiveSlotPreview();
    }

    public static bool TryCreateInitialSave(PlayerCharacter player)
    {
        if (!initialSavePending || player == null)
            return false;

        initialSavePending = false;
        SavePlayer(player);
        return true;
    }

    public static void SetContinueScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        HasSession = true;
        ContinueSceneName = sceneName;
    }

    public static void SaveQuestProgress()
    {
        HasSession = true;
    }

    public static void SaveDialogueProgress()
    {
        HasSession = true;
    }

    public static void SaveStoryFlags()
    {
        HasSession = true;
    }

    public static void SaveWallet()
    {
        HasSession = true;
    }

    public static void SaveShopProgress()
    {
        HasSession = true;
    }

    public static void SaveSkillProgression()
    {
        HasSession = true;
    }

    public static bool TryGetNpcPosition(string npcKey, out Vector3 position)
    {
        if (string.IsNullOrWhiteSpace(npcKey))
        {
            position = default;
            return false;
        }

        return npcPositions.TryGetValue(npcKey, out position);
    }

    public static void SaveNpcPosition(string npcKey, Vector3 position)
    {
        if (string.IsNullOrWhiteSpace(npcKey))
            return;

        npcPositions[npcKey] = position;
        HasSession = true;
    }

    public static void LoadPersistentSaveIfNeeded()
    {
        if (persistentLoaded)
            return;

        persistentLoaded = true;

        if (!PersistentSessionSave.TryLoad(out SessionSaveData data))
            return;

        loadedPersistentData = data;
        RestoreFrom(data);
    }

    private static void PersistCurrentSession()
    {
        if (!HasSession)
            return;

        PersistentSessionSave.Save(CreateSaveData());
    }

    public static bool HasPersistentSessionInSlot(int slotIndex)
    {
        return PersistentSessionSave.HasSlot(slotIndex);
    }

    public static void StartNewPersistentSlot(int slotIndex)
    {
        PersistentSessionSave.SetActiveSlot(slotIndex);
        Clear();
        initialSavePending = true;
    }

    public static bool LoadPersistentSlot(int slotIndex)
    {
        PersistentSessionSave.SetActiveSlot(slotIndex);
        ResetRuntimeState();
        persistentLoaded = true;

        if (!PersistentSessionSave.TryLoad(out SessionSaveData data))
            return false;

        loadedPersistentData = data;
        RestoreFrom(data);
        return HasSession;
    }

    public static void Clear()
    {
        ResetRuntimeState();
        PersistentSessionSave.Delete();
    }

    private static void ResetRuntimeState()
    {
        savedPosition = default;
        hasSavedPosition = false;
        savedHealth = default;
        hasSavedHealth = false;
        runtimeInventory = null;
        runtimeDeck = null;
        npcPositions.Clear();
        initialSavePending = false;
        loadedPersistentData = null;
        HasSession = false;
        ContinueSceneName = SceneNames.Exploration;
        PendingBattleEnemy.ClearSession();
        DefeatedEncounters.Clear();
        EncounterGroups.Clear();
        CollectedPickups.Clear();
        QuestManager.Clear();
        ShopManager.Clear();
        PlayerWallet.Clear();
        SkillProgression.Clear();
        NpcDialogueProgress.Clear();
        StoryFlagManager.Clear();
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
            PlayerWallet.IsInitialized,
            PlayerWallet.Gold,
            runtimeInventory,
            runtimeDeck,
            QuestManager.ToArray(),
            ShopManager.ToArray(),
            CreateNpcPositionSaveData(),
            NpcDialogueProgress.ToArray(),
            SkillProgression.IsInitialized,
            SkillProgression.AvailablePoints,
            SkillProgression.ToArray(),
            StoryFlagManager.ToArray());
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

        PlayerWallet.Restore(data.hasGold, data.gold);

        int[] enemyHealths = data.enemyHealths != null && data.enemyHealths.Length > 0
            ? data.enemyHealths
            : data.hasEnemyHealth ? new[] { data.enemyHealth } : null;
        PendingBattleEnemy.RestoreSession(data.pendingEncounterId, enemyHealths);

        DefeatedEncounters.Restore(data.defeatedEncounterIds);
        EncounterGroups.Restore(data.encounterGroups);
        CollectedPickups.Restore(data.collectedPickupIds);
        QuestManager.Restore(data.questProgress);
        ShopManager.Restore(data.shopProgress);
        SkillProgression.Restore(data.hasSkillProgression, data.skillPoints, data.skillProgression);
        NpcDialogueProgress.Restore(data.npcDialogueProgress);
        StoryFlagManager.Restore(data.storyFlags);
        RestoreNpcPositions(data.npcPositions);
    }

    private static NpcPositionSaveData[] CreateNpcPositionSaveData()
    {
        SaveNpcPositions();

        NpcPositionSaveData[] result = new NpcPositionSaveData[npcPositions.Count];
        int index = 0;
        foreach (KeyValuePair<string, Vector3> entry in npcPositions)
        {
            result[index] = new NpcPositionSaveData
            {
                npcKey = entry.Key,
                positionX = entry.Value.x,
                positionY = entry.Value.y,
                positionZ = entry.Value.z
            };
            index++;
        }

        return result;
    }

    // Remembers where every NPC in the scene stands, e.g. before a battle, so they are back there
    // (the companion next to the player) when the scene loads again.
    public static void SaveNpcPositions()
    {
        NpcCharacter[] npcs = Object.FindObjectsByType<NpcCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < npcs.Length; i++)
        {
            NpcCharacter npc = npcs[i];
            if (npc == null)
                continue;

            string key = npc.PersistenceKey;
            if (!string.IsNullOrWhiteSpace(key))
                npcPositions[key] = npc.transform.position;
        }
    }

    private static void RestoreNpcPositions(NpcPositionSaveData[] savedNpcPositions)
    {
        npcPositions.Clear();
        if (savedNpcPositions == null)
            return;

        for (int i = 0; i < savedNpcPositions.Length; i++)
        {
            NpcPositionSaveData savedNpc = savedNpcPositions[i];
            if (savedNpc == null || string.IsNullOrWhiteSpace(savedNpc.npcKey))
                continue;

            npcPositions[savedNpc.npcKey] = new Vector3(savedNpc.positionX, savedNpc.positionY, savedNpc.positionZ);
        }
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
