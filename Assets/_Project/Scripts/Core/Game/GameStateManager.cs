public static class GameStateManager
{
    public static GameState State = GameState.Gameplay;

    public static bool IsGameplay => State == GameState.Gameplay;
}