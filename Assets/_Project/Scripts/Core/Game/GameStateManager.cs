using UnityEngine.SceneManagement;

public static class GameStateManager
{
    public static GameState State = GameState.Gameplay;

    public static bool IsGameplay => State == GameState.Gameplay;
    public static bool IsBattleScene => SceneManager.GetActiveScene().name == SceneNames.Battle;

    public static bool CanOpenInventory()
    {
        if (!IsBattleScene)
            return true;

        BattleSystem battleSystem = UnityEngine.Object.FindFirstObjectByType<BattleSystem>();
        return battleSystem != null && battleSystem.CurrentBattleState == BattleState.Running;
    }

    public static bool CanEditCardCollections()
    {
        return !IsBattleScene;
    }

    public static void SetState(GameState state)
    {
        State = state;
        UnityEngine.Time.timeScale = state == GameState.Inventory ? 0f : 1f;
    }
}
