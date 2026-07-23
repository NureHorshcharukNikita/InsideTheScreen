public static class MainMenuNavigation
{
    public static void GoToMainMenu()
    {
        GameStateManager.SetState(GameState.Gameplay);
        FadeManager.TryFadeToScene(SceneNames.MainMenu);
    }
}
