using System.Collections;
using UnityEngine;

public sealed class MainMenuIntroAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform title;
    [SerializeField] private CanvasGroup buttons;
    [SerializeField, Min(0f)] private float titleStartOffset = 420f;
    [SerializeField, Min(0.01f)] private float titleDuration = 1.65f;
    [SerializeField, Min(0f)] private float buttonsDelay = 0.55f;
    [SerializeField, Min(0.01f)] private float buttonsDuration = 1.1f;

    private static bool hasPlayed;
    private static bool replayRequested;
    private Vector2 titleTargetPosition;
    private bool shouldAnimate;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        hasPlayed = false;
        replayRequested = false;
    }

    public static void RequestReplay()
    {
        replayRequested = true;
    }

    private void Awake()
    {
        if (title == null || buttons == null)
            return;

        titleTargetPosition = title.anchoredPosition;
        shouldAnimate = !hasPlayed || replayRequested;
        hasPlayed = true;
        replayRequested = false;

        if (!shouldAnimate)
        {
            ApplyFinalState();
            return;
        }

        title.anchoredPosition = titleTargetPosition + Vector2.up * titleStartOffset;
        buttons.alpha = 0f;
        buttons.interactable = false;
        buttons.blocksRaycasts = false;
    }

    private IEnumerator Start()
    {
        if (!shouldAnimate || title == null || buttons == null)
            yield break;

        float elapsed = 0f;
        float totalDuration = Mathf.Max(titleDuration, buttonsDelay + buttonsDuration);
        Vector2 titleStartPosition = titleTargetPosition + Vector2.up * titleStartOffset;

        while (elapsed < totalDuration)
        {
            elapsed += GameClock.DeltaTime;

            float titleProgress = Mathf.Clamp01(elapsed / titleDuration);
            float easedTitleProgress = 1f - Mathf.Pow(1f - titleProgress, 3f);
            title.anchoredPosition = Vector2.LerpUnclamped(
                titleStartPosition,
                titleTargetPosition,
                easedTitleProgress);

            float buttonsProgress = Mathf.Clamp01((elapsed - buttonsDelay) / buttonsDuration);
            buttons.alpha = Mathf.SmoothStep(0f, 1f, buttonsProgress);
            yield return null;
        }

        ApplyFinalState();
    }

    private void ApplyFinalState()
    {
        title.anchoredPosition = titleTargetPosition;
        buttons.alpha = 1f;
        buttons.interactable = true;
        buttons.blocksRaycasts = true;
    }
}
