public static class MainMenuNavigation
{
    public static void GoToMainMenu()
    {
        GameStateManager.SetState(GameState.Gameplay);
        MainMenuIntroAnimation.RequestReplay();
        FadeManager.TryFadeToScene(SceneNames.MainMenu);
    }
}
