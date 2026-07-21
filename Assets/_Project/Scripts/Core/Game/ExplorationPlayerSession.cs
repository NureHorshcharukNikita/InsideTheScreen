using UnityEngine;

public static class ExplorationPlayerSession
{
    private static Vector3 savedPosition;
    private static bool hasSavedPosition;
    private static int savedHealth;
    private static bool hasSavedHealth;
    private static InventoryData runtimeInventory;
    private static DeckData runtimeDeck;

    public static bool HasSession { get; private set; }
    public static string ContinueSceneName { get; private set; } = SceneNames.Exploration;

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
            runtimeInventory = template.CreateRuntimeCopy();

        HasSession = true;
        return runtimeInventory;
    }

    public static DeckData GetOrCreateRuntimeDeck(DeckData template)
    {
        if (template == null)
            return null;

        if (runtimeDeck == null)
            runtimeDeck = template.CreateRuntimeCopy();

        HasSession = true;
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
    }

    public static void SaveBattle(PlayerCharacter player, EnemyCharacter enemy)
    {
        SaveBattle(player);

        if (enemy != null)
            PendingBattleEnemy.SaveCurrentEnemyHealth(enemy.CurrentHealth);
    }

    public static void SetContinueScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        HasSession = true;
        ContinueSceneName = sceneName;
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
    }
}
