using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsInputPanel : MonoBehaviour
{
    [SerializeField] private SettingsKeyBindingRow[] bindingRows;
    [SerializeField] private SettingsAudioRow[] audioRows;
    [SerializeField] private Button controlsTabButton;
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private TMP_Text sectionTitle;
    [SerializeField] private GameObject controlsContent;
    [SerializeField] private GameObject audioContent;
    [SerializeField] private Image controlsTabImage;
    [SerializeField] private Image audioTabImage;
    [SerializeField] private Sprite controlsNormalSprite;
    [SerializeField] private Sprite controlsActiveSprite;
    [SerializeField] private Sprite audioNormalSprite;
    [SerializeField] private Sprite audioActiveSprite;
    [SerializeField] private Image actionButtonImage;
    [SerializeField] private Sprite resetButtonSprite;
    [SerializeField] private ScrollRect controlsScrollRect;
    [SerializeField] private ScrollRect audioScrollRect;

    [Header("Animation")]
    [SerializeField] private RectTransform animatedPanel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Min(0f)] private float verticalOffset = 90f;
    [SerializeField, Min(0.01f)] private float enterDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float exitDuration = 0.5f;

    private bool controlsActive;
    private bool isExiting;
    private Coroutine panelAnimationRoutine;
    private Vector2 visiblePosition;
    private Vector2 sectionTitlePosition;

    private void Awake()
    {
        if (animatedPanel == null)
            animatedPanel = transform as RectTransform;
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (animatedPanel != null)
            visiblePosition = animatedPanel.anchoredPosition;
        if (sectionTitle != null)
            sectionTitlePosition = sectionTitle.rectTransform.anchoredPosition;

        if (controlsTabButton != null)
            controlsTabButton.onClick.AddListener(ShowControls);

        if (audioTabButton != null)
            audioTabButton.onClick.AddListener(ShowAudio);

        if (applyButton != null)
            applyButton.onClick.AddListener(HandleActionButton);

        Refresh();
    }

    private void OnEnable()
    {
        GameInputSettings.Changed += Refresh;
        ResetToDefaultView();
        Refresh();
        isExiting = false;
        StartPanelAnimation(PlayEnter());
    }

    private void OnDisable()
    {
        GameInputSettings.Changed -= Refresh;

        if (panelAnimationRoutine != null)
            StopCoroutine(panelAnimationRoutine);

        panelAnimationRoutine = null;
        isExiting = false;
    }

    public void ShowControls()
    {
        SetActiveTab(true);
        ResetScrollPosition(controlsScrollRect);
    }

    public void ShowAudio()
    {
        SetActiveTab(false);
        ResetScrollPosition(audioScrollRect);
    }

    public void ResetToDefaultView()
    {
        SetActiveTab(true);
        ResetAllScrollPositions();
        StartCoroutine(ResetScrollPositionsAfterLayout());
    }

    public void PlayExit(Action onComplete)
    {
        if (isExiting)
            return;

        isExiting = true;
        StartPanelAnimation(PlayExitRoutine(onComplete));
    }

    public void ResetAudio()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ResetVolumes();
        Refresh();
    }

    public void ResetControls()
    {
        GameInputSettings.ResetToDefaults();
        Refresh();
    }

    public void Refresh()
    {
        if (bindingRows == null)
            return;

        foreach (SettingsKeyBindingRow row in bindingRows)
        {
            if (row != null)
                row.Refresh();
        }

        if (audioRows == null)
            return;

        foreach (SettingsAudioRow row in audioRows)
        {
            if (row != null)
                row.Refresh();
        }
    }

    private void SetActiveTab(bool controlsActive)
    {
        this.controlsActive = controlsActive;

        if (sectionTitle != null)
        {
            sectionTitle.text = controlsActive ? "Keyboard Bindings" : "Audio Settings";
            sectionTitle.rectTransform.anchoredPosition = sectionTitlePosition;
        }

        if (controlsContent != null)
            controlsContent.SetActive(controlsActive);

        if (audioContent != null)
            audioContent.SetActive(!controlsActive);

        if (controlsTabImage != null)
            controlsTabImage.sprite = controlsActive ? controlsActiveSprite : controlsNormalSprite;

        if (audioTabImage != null)
            audioTabImage.sprite = controlsActive ? audioNormalSprite : audioActiveSprite;

        if (actionButtonImage != null)
            actionButtonImage.sprite = resetButtonSprite;
    }

    private void HandleActionButton()
    {
        if (controlsActive)
        {
            ResetControls();
            return;
        }

        ResetAudio();
    }

    private void ResetAllScrollPositions()
    {
        Canvas.ForceUpdateCanvases();
        ResetScrollPosition(controlsScrollRect);
        ResetScrollPosition(audioScrollRect);
    }

    private IEnumerator ResetScrollPositionsAfterLayout()
    {
        yield return null;
        ResetAllScrollPositions();
    }

    private static void ResetScrollPosition(ScrollRect scrollRect)
    {
        if (scrollRect == null)
            return;

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private IEnumerator PlayEnter()
    {
        if (animatedPanel == null || canvasGroup == null)
            yield break;

        Vector2 hiddenPosition = visiblePosition + Vector2.down * verticalOffset;
        animatedPanel.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;
        SetInteraction(false);

        yield return AnimatePanel(hiddenPosition, visiblePosition, 0f, 1f, enterDuration, true);

        animatedPanel.anchoredPosition = visiblePosition;
        canvasGroup.alpha = 1f;
        SetInteraction(true);
        panelAnimationRoutine = null;
    }

    private IEnumerator PlayExitRoutine(Action onComplete)
    {
        if (animatedPanel == null || canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        SetInteraction(false);
        Vector2 startPosition = animatedPanel.anchoredPosition;
        Vector2 hiddenPosition = visiblePosition + Vector2.down * verticalOffset;

        yield return AnimatePanel(
            startPosition,
            hiddenPosition,
            canvasGroup.alpha,
            0f,
            exitDuration,
            false);

        animatedPanel.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;
        panelAnimationRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator AnimatePanel(
        Vector2 fromPosition,
        Vector2 toPosition,
        float fromAlpha,
        float toAlpha,
        float duration,
        bool easeOut)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GameClock.DeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = easeOut
                ? 1f - Mathf.Pow(1f - progress, 3f)
                : Mathf.SmoothStep(0f, 1f, progress);

            animatedPanel.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, easedProgress);
            canvasGroup.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, easedProgress);
            yield return null;
        }
    }

    private void StartPanelAnimation(IEnumerator routine)
    {
        if (panelAnimationRoutine != null)
            StopCoroutine(panelAnimationRoutine);

        panelAnimationRoutine = StartCoroutine(routine);
    }

    private void SetInteraction(bool enabled)
    {
        canvasGroup.blocksRaycasts = enabled;
    }
}
