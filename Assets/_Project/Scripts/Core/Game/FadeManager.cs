using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    private static bool shouldFadeInAfterSceneLoad;

    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private bool fadeInOnSceneStart;
    [SerializeField] private bool persistRootOnAwake;

    private FadeOverlayController _overlay;
    private bool _isFading;
    private bool _fadeInFromBlackOnStart;

    public bool IsFading => _isFading;

    public static void TryFadeToScene(string sceneName)
    {
        if (Instance != null)
            Instance.FadeToScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public static void TryLoadSceneWithoutFade(string sceneName)
    {
        if (Instance != null)
            Instance.LoadSceneWithoutFade(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public static void MarkFadeInAfterNextSceneLoad()
    {
        shouldFadeInAfterSceneLoad = true;
    }

    public static bool TryFadeWithinScene(Action onBlack, Action onComplete)
    {
        if (Instance == null || Instance._overlay == null || !Instance._overlay.IsAvailable)
        {
            onBlack?.Invoke();
            onComplete?.Invoke();
            return true;
        }

        if (Instance._isFading)
            return false;

        Instance.StartCoroutine(FadeSceneTransition.FadeToBlackThenAct(
            Instance._overlay,
            Instance.fadeDuration,
            onBlack,
            value => Instance._isFading = value,
            onComplete));
        return true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            new FadeOverlayController(fadeGroup).Hide();
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (persistRootOnAwake)
            DontDestroyOnLoad(transform.root.gameObject);

        _fadeInFromBlackOnStart = shouldFadeInAfterSceneLoad || fadeInOnSceneStart;
        shouldFadeInAfterSceneLoad = false;

        _overlay = new FadeOverlayController(fadeGroup);

        if (_fadeInFromBlackOnStart)
            _overlay.ShowBlack();
        else
            _overlay.Hide();

        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        if (_fadeInFromBlackOnStart && fadeGroup != null)
            StartCoroutine(FadeSceneTransition.FadeInFromBlack(
                _overlay,
                fadeDuration,
                value => _isFading = value));
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!shouldFadeInAfterSceneLoad)
            return;

        shouldFadeInAfterSceneLoad = false;
        _overlay?.ShowBlack();
        StartCoroutine(FadeSceneTransition.FadeInFromBlack(
            _overlay,
            fadeDuration,
            value => _isFading = value));
    }

    public void FadeToScene(string sceneName)
    {
        if (_isFading)
            return;

        if (_overlay == null || !_overlay.IsAvailable)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(FadeSceneTransition.FadeToBlackThenLoad(
            _overlay,
            fadeDuration,
            sceneName,
            value => _isFading = value,
            () => shouldFadeInAfterSceneLoad = true));
    }

    public void LoadSceneWithoutFade(string sceneName)
    {
        StopAllCoroutines();
        shouldFadeInAfterSceneLoad = false;
        _isFading = false;
        _overlay?.Hide();
        SceneManager.LoadScene(sceneName);
    }

    public CanvasGroup FadeOverlayGroup => fadeGroup;
}
