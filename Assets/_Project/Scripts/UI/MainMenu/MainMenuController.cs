using UnityEngine;
using UnityEngine.Video;
using UnityEngine.EventSystems;
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
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Video")]
    [SerializeField] private VideoPlayer menuVideoPlayer;

    private bool menuVideoPausedByFocusLoss;

    private void Awake()
    {
        ExplorationPlayerSession.LoadPersistentSaveIfNeeded();
        GameStateManager.SetState(GameState.Gameplay);
        CacheMenuVideoPlayer();

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
        ClearSelectedButton();
        if (menuPanel != null)
            menuPanel.SetActive(false);
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void HideSettings()
    {
        ClearSelectedButton();
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    public void ShowCredits()
    {
        ClearSelectedButton();
        if (menuPanel != null)
            menuPanel.SetActive(false);
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void HideCredits()
    {
        ClearSelectedButton();
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetMenuVideoFocusState(hasFocus);
    }

    private void OnApplicationPause(bool isPaused)
    {
        SetMenuVideoFocusState(!isPaused);
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null)
            continueButton.interactable = ExplorationPlayerSession.HasSession;
    }

    private void CacheMenuVideoPlayer()
    {
        if (menuVideoPlayer == null)
            menuVideoPlayer = FindFirstObjectByType<VideoPlayer>();
    }

    private void SetMenuVideoFocusState(bool hasFocus)
    {
        if (menuVideoPlayer == null)
            return;

        if (!hasFocus)
        {
            if (menuVideoPausedByFocusLoss)
                return;

            menuVideoPausedByFocusLoss = true;
            menuVideoPlayer.Pause();
            return;
        }

        if (!menuVideoPausedByFocusLoss)
            return;

        menuVideoPausedByFocusLoss = false;
        menuVideoPlayer.Play();
    }

    private static void ClearSelectedButton()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
