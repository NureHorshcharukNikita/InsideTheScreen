public static class GameStateManager
{
    public static GameState State = GameState.Gameplay;

    public static bool IsGameplay => State == GameState.Gameplay;

    public static void SetState(GameState state)
    {
        State = state;
        UnityEngine.Time.timeScale = state == GameState.Inventory ? 0f : 1f;
    }
}
