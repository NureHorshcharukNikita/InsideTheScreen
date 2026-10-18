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
    [SerializeField] private GameObject menuTitle;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private MainMenuSaveSlotsPanel saveSlotsPanel;

    [Header("Video")]
    [SerializeField] private VideoPlayer menuVideoPlayer;
    [SerializeField] private AudioSource menuVideoAudioSource;
    [Tooltip("The menu sound rises from silence to its volume over this time whenever the menu opens (game start, back from the game), so it does not hit the ears at once. 0 = full volume at once.")]
    [SerializeField, Min(0f)] private float menuSoundFadeInDuration = 3f;

    private bool menuVideoPausedByFocusLoss;
    // 0..1 of the way through the fade-in.
    private float menuSoundFade = 1f;
    private SettingsInputPanel settingsInputPanel;

    private void Awake()
    {
        ExplorationPlayerSession.LoadPersistentSaveIfNeeded();
        GameStateManager.SetState(GameState.Gameplay);
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopAmbience();

        CacheMenuVideoPlayer();
        menuSoundFade = menuSoundFadeInDuration > 0f ? 0f : 1f;
        // Silent even if the AudioManager is not up yet and the volume below is skipped.
        if (menuVideoAudioSource != null && menuSoundFade < 1f)
            menuVideoAudioSource.volume = 0f;
        ApplyMenuVideoVolume();

        if (settingsPanel != null)
            settingsInputPanel = settingsPanel.GetComponent<SettingsInputPanel>();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
        if (saveSlotsPanel != null)
            saveSlotsPanel.gameObject.SetActive(false);

        RefreshContinueButton();
    }

    private void OnEnable()
    {
        AudioManager.VolumesChanged += ApplyMenuVideoVolume;
        ApplyMenuVideoVolume();
    }

    private void OnDisable()
    {
        AudioManager.VolumesChanged -= ApplyMenuVideoVolume;
    }

    // The fade-in starts once the video plays: it waits for its first frame, and the sound with it.
    private void Update()
    {
        if (menuSoundFade >= 1f || (menuVideoPlayer != null && !menuVideoPlayer.isPlaying))
            return;

        menuSoundFade = Mathf.MoveTowards(menuSoundFade, 1f, Time.unscaledDeltaTime / menuSoundFadeInDuration);
        ApplyMenuVideoVolume();
    }

    public void StartNewGame()
    {
        ShowSaveSlots(MainMenuSaveSlotMode.NewGame);
    }

    public void StartNewGameInSlot(int slotIndex)
    {
        ExplorationPlayerSession.StartNewPersistentSlot(slotIndex);
        PendingBattleEnemy.ClearSession();
        DefeatedEncounters.Clear();
        EncounterGroups.Clear();
        EnemyEncounter.ResetEncounterIds();
        GameStateManager.SetState(GameState.Gameplay);
        LoadingScreenRequest.SetTargetScene(SceneNames.Exploration);
        FadeManager.TryFadeToScene(SceneNames.Loading);
    }

    public void ContinueGame()
    {
        if (!ExplorationPlayerSession.HasAnyPersistentSession)
            return;

        ShowSaveSlots(MainMenuSaveSlotMode.Continue);
    }

    public void ContinueGameFromSlot(int slotIndex)
    {
        if (!ExplorationPlayerSession.LoadPersistentSlot(slotIndex))
            return;

        string sceneName = ExplorationPlayerSession.ContinueSceneName;
        if (sceneName == SceneNames.Exploration)
            EnemyEncounter.ResetEncounterIds();

        GameStateManager.SetState(GameState.Gameplay);
        LoadingScreenRequest.SetTargetScene(sceneName);
        FadeManager.TryFadeToScene(SceneNames.Loading);
    }

    public void HideSaveSlots()
    {
        ClearSelectedButton();

        if (saveSlotsPanel != null && saveSlotsPanel.gameObject.activeSelf)
        {
            saveSlotsPanel.PlayExit(CompleteHideSaveSlots);
            return;
        }

        CompleteHideSaveSlots();
    }

    private void CompleteHideSaveSlots()
    {
        if (saveSlotsPanel != null)
            saveSlotsPanel.gameObject.SetActive(false);
        if (menuTitle != null)
            menuTitle.SetActive(true);
        if (menuPanel != null)
            menuPanel.SetActive(true);
    }

    public void ShowSettings()
    {
        ClearSelectedButton();
        if (menuTitle != null)
            menuTitle.SetActive(false);
        if (menuPanel != null)
            menuPanel.SetActive(false);
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void HideSettings()
    {
        ClearSelectedButton();

        if (settingsInputPanel != null && settingsPanel != null && settingsPanel.activeSelf)
        {
            settingsInputPanel.PlayExit(CompleteHideSettings);
            return;
        }

        CompleteHideSettings();
    }

    private void CompleteHideSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (menuTitle != null)
            menuTitle.SetActive(true);
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
            continueButton.interactable = ExplorationPlayerSession.HasAnyPersistentSession;
    }

    private void ShowSaveSlots(MainMenuSaveSlotMode mode)
    {
        ClearSelectedButton();
        if (menuTitle != null)
            menuTitle.SetActive(false);
        if (menuPanel != null)
            menuPanel.SetActive(false);
        if (saveSlotsPanel != null)
            saveSlotsPanel.Show(this, mode);
    }

    private void CacheMenuVideoPlayer()
    {
        if (menuVideoPlayer == null)
            menuVideoPlayer = FindFirstObjectByType<VideoPlayer>();

        if (menuVideoAudioSource == null && menuVideoPlayer != null)
            menuVideoAudioSource = menuVideoPlayer.GetComponent<AudioSource>();
    }

    private void ApplyMenuVideoVolume()
    {
        CacheMenuVideoPlayer();

        if (menuVideoAudioSource == null || AudioManager.Instance == null)
            return;

        // Squared: the ear hears a linear rise as a jump at the start.
        menuVideoAudioSource.volume = AudioManager.Instance.GetOutputVolume(AudioChannel.Music) * menuSoundFade * menuSoundFade;
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
