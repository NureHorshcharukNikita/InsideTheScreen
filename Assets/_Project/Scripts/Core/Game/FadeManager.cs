using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    private static bool shouldFadeInAfterSceneLoad;

    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    private FadeOverlayController _overlay;
    private bool _isFading;
    private bool _fadeInFromBlackOnStart;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _fadeInFromBlackOnStart = shouldFadeInAfterSceneLoad;
        shouldFadeInAfterSceneLoad = false;

        _overlay = new FadeOverlayController(fadeGroup);

        if (_fadeInFromBlackOnStart)
            _overlay.ShowBlack();
        else
            _overlay.Hide();
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
            Instance = null;
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
