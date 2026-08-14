using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private Image progressFill;
    [SerializeField] private RectTransform progressFillRect;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private RectTransform spinner;
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float fadeDuration = 0.45f;
    [SerializeField] private float minimumLoadingTime = 5f;
    [SerializeField, Range(0.01f, 1f)] private float minimumTimeProgressLimit = 0.95f;
    [SerializeField] private float progressFillSpeed = 1.2f;
    [SerializeField] private float spinnerDegreesPerSecond = -240f;
    [SerializeField] private float titleAnimationInterval = 0.45f;
    [SerializeField] private float tipChangeInterval = 2.5f;
    [SerializeField] private string[] tips;

    private int currentTipIndex = -1;
    private int titleFrameIndex;

    private void Awake()
    {
        if (progressFillRect == null && progressFill != null)
            progressFillRect = progressFill.rectTransform;

        SetProgress(0f);
        ShowNextTip();
    }

    private void Start()
    {
        StartCoroutine(LoadTargetScene());
        StartCoroutine(AnimateTitle());
        StartCoroutine(RotateTips());
    }

    private void Update()
    {
        if (spinner != null)
            spinner.Rotate(0f, 0f, spinnerDegreesPerSecond * Time.unscaledDeltaTime);
    }

    private IEnumerator LoadTargetScene()
    {
        string targetScene = LoadingScreenRequest.ConsumeTargetScene();
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);

        if (operation == null)
        {
            Debug.LogError($"Could not start loading scene '{targetScene}'.");
            yield break;
        }

        operation.allowSceneActivation = false;

        float startedAt = Time.unscaledTime;
        float displayedProgress = 0f;
        bool activationStarted = false;

        while (!operation.isDone)
        {
            float elapsed = Time.unscaledTime - startedAt;
            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float timeProgress = minimumLoadingTime > 0f
                ? Mathf.Clamp01(elapsed / minimumLoadingTime) * minimumTimeProgressLimit
                : minimumTimeProgressLimit;
            bool sceneReady = operation.progress >= 0.9f;
            bool minimumTimePassed = elapsed >= minimumLoadingTime;
            float targetProgress = sceneReady && minimumTimePassed
                ? 1f
                : Mathf.Min(realProgress, timeProgress);

            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                targetProgress,
                progressFillSpeed * Time.unscaledDeltaTime);

            SetProgress(displayedProgress);

            if (!activationStarted && sceneReady && minimumTimePassed && displayedProgress >= 0.99f)
            {
                activationStarted = true;
                SetProgress(1f);
                yield return Fade(0f, 1f);
                FadeManager.MarkFadeInAfterNextSceneLoad();
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void SetProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (progressFillRect != null)
        {
            Vector2 anchorMax = progressFillRect.anchorMax;
            anchorMax.x = progress;
            progressFillRect.anchorMax = anchorMax;
            progressFillRect.offsetMin = Vector2.zero;
            progressFillRect.offsetMax = Vector2.zero;
        }

        if (progressFill != null)
            progressFill.fillAmount = progressFillRect != null ? 1f : progress;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeGroup == null && FadeManager.Instance != null)
            fadeGroup = FadeManager.Instance.FadeOverlayGroup;

        if (fadeGroup == null || fadeDuration <= 0f)
        {
            SetFadeAlpha(to);
            yield break;
        }

        fadeGroup.gameObject.SetActive(true);
        fadeGroup.blocksRaycasts = true;
        fadeGroup.interactable = false;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }

        SetFadeAlpha(to);

        if (Mathf.Approximately(to, 0f))
        {
            fadeGroup.blocksRaycasts = false;
            fadeGroup.gameObject.SetActive(false);
        }
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeGroup == null)
            return;

        fadeGroup.alpha = Mathf.Clamp01(alpha);
    }

    private IEnumerator RotateTips()
    {
        if (tips == null || tips.Length <= 1 || tipChangeInterval <= 0f)
            yield break;

        while (true)
        {
            yield return new WaitForSecondsRealtime(tipChangeInterval);
            ShowNextTip();
        }
    }

    private IEnumerator AnimateTitle()
    {
        if (titleText == null || titleAnimationInterval <= 0f)
            yield break;

        while (true)
        {
            titleText.text = titleFrameIndex switch
            {
                1 => "Loading.",
                2 => "Loading..",
                3 => "Loading...",
                _ => "Loading"
            };

            titleFrameIndex = (titleFrameIndex + 1) % 4;
            yield return new WaitForSecondsRealtime(titleAnimationInterval);
        }
    }

    private void ShowNextTip()
    {
        if (tipText == null || tips == null || tips.Length == 0)
            return;

        if (currentTipIndex < 0)
            currentTipIndex = Random.Range(0, tips.Length);
        else
            currentTipIndex = (currentTipIndex + 1) % tips.Length;

        tipText.text = tips[currentTipIndex];
    }
}
