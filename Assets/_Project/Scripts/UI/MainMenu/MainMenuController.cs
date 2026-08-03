using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private Button closeCreditsButton;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    private void Awake()
    {
        ExplorationPlayerSession.LoadPersistentSaveIfNeeded();
        GameStateManager.SetState(GameState.Gameplay);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        RefreshContinueButton();
    }

    public void StartNewGame()
    {
        ExplorationPlayerSession.Clear();
        PendingBattleEnemy.ClearSession();
        DefeatedEncounters.Clear();
        EnemyEncounter.ResetEncounterIds();
        GameStateManager.SetState(GameState.Gameplay);
        FadeManager.TryFadeToScene(SceneNames.Exploration);
    }

    public void ContinueGame()
    {
        if (!ExplorationPlayerSession.HasSession)
            return;

        string sceneName = ExplorationPlayerSession.ContinueSceneName;
        if (sceneName == SceneNames.Exploration)
            EnemyEncounter.ResetEncounterIds();

        GameStateManager.SetState(GameState.Gameplay);
        FadeManager.TryFadeToScene(sceneName);
    }

    public void ShowSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void HideSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void HideCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null)
            continueButton.interactable = ExplorationPlayerSession.HasSession;
    }
}
