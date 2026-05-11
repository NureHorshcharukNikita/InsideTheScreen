using UnityEngine;
using UnityEngine.SceneManagement;

public partial class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    private static bool s_pendingFadeInAfterLoad;

    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeDuration = 0.5f;

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

        _fadeInFromBlackOnStart = s_pendingFadeInAfterLoad;
        s_pendingFadeInAfterLoad = false;

        if (fadeGroup == null)
            return;

        if (_fadeInFromBlackOnStart)
        {
            fadeGroup.gameObject.SetActive(true);
            fadeGroup.alpha = 1f;
            fadeGroup.blocksRaycasts = true;
            fadeGroup.interactable = false;
        }
        else
            HideFadeOverlay();
    }

    private void Start()
    {
        if (_fadeInFromBlackOnStart && fadeGroup != null)
            StartCoroutine(FadeInFromBlackRoutine());
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

        if (fadeGroup == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(FadeToBlackThenLoadRoutine(sceneName));
    }

    public void LoadSceneWithoutFade(string sceneName)
    {
        StopAllCoroutines();
        s_pendingFadeInAfterLoad = false;
        _isFading = false;
        HideFadeOverlay();
        SceneManager.LoadScene(sceneName);
    }

    public CanvasGroup FadeOverlayGroup => fadeGroup;

    private void HideFadeOverlay()
    {
        if (fadeGroup == null)
            return;
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;
        fadeGroup.gameObject.SetActive(false);
    }

}
