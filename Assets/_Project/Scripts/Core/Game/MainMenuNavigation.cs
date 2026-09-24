public static class MainMenuNavigation
{
    private const float AmbienceFadeDuration = 0.5f;

    public static void GoToMainMenu()
    {
        GameStateManager.SetState(GameState.Gameplay);
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopAmbience(AmbienceFadeDuration);

        MainMenuIntroAnimation.RequestReplay();
        FadeManager.TryFadeToScene(SceneNames.MainMenu);
    }
}
