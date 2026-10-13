using UnityEngine.SceneManagement;

public static class GameStateManager
{
    public static GameState State = GameState.Gameplay;
    public static int LastStateChangeFrame { get; private set; } = -1;

    public static bool IsGameplay => State == GameState.Gameplay;
    public static bool IsBattleScene => SceneManager.GetActiveScene().name == SceneNames.Battle;

    public static bool CanOpenInventory()
    {
        return IsGameplay && !IsBattleOver();
    }

    // Esc and losing the window's focus alike.
    public static bool CanPause()
    {
        return IsGameplay && LastStateChangeFrame != UnityEngine.Time.frameCount && !IsBattleOver();
    }

    // Won or lost: the battle end panel is up, and neither the pause menu nor the inventory opens
    // over it.
    private static bool IsBattleOver()
    {
        if (!IsBattleScene)
            return false;

        BattleSystem battleSystem = UnityEngine.Object.FindFirstObjectByType<BattleSystem>();
        return battleSystem == null || battleSystem.CurrentBattleState != BattleState.Running;
    }

    public static bool CanEditCardCollections()
    {
        return !IsBattleScene;
    }

    public static void SetState(GameState state)
    {
        if (State != state)
            LastStateChangeFrame = UnityEngine.Time.frameCount;

        State = state;
        UnityEngine.Time.timeScale = state == GameState.Paused
            || state == GameState.Inventory
            || state == GameState.Dialogue
            || state == GameState.Shop
            ? 0f
            : 1f;
    }
}
