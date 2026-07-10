using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class BattleTurnUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TMP_Text turnText;

    [Header("Labels")]
    [SerializeField] private string playerTurnLabel = "YOUR TURN";
    [SerializeField] private string enemyTurnLabel = "ENEMY TURN";

    [Header("Timing")]
    [SerializeField] private float slideInDuration = 0.45f;
    [SerializeField] private float slideOutDuration = 0.4f;
    [SerializeField] private float playerHoldDuration = 1f;
    [SerializeField] private float enemyHoldDuration = 0.75f;
    [SerializeField] private float slideOvershoot = 80f;

    private Vector2 restPosition;
    private Tween activeTween;

    private void Awake()
    {
        if (panelRect == null)
            panelRect = transform as RectTransform;

        if (turnText == null)
            turnText = GetComponentInChildren<TMP_Text>(true);

        if (panelRect != null)
            restPosition = panelRect.anchoredPosition;

        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        KillActiveTween();
    }

    public void ForceHide()
    {
        KillActiveTween();
        gameObject.SetActive(false);
    }

    public void RequestSkip()
    {
        localSkipRequested = true;
        KillActiveTween();
        gameObject.SetActive(false);
    }

    public IEnumerator PlayTurnAnnouncement(TurnOwner owner, Func<bool> shouldSkip = null)
    {
        if (panelRect == null || turnText == null)
            yield break;

        localSkipRequested = false;
        KillActiveTween();

        if (IsSkipped(shouldSkip))
            yield break;

        turnText.text = owner == TurnOwner.Player ? playerTurnLabel : enemyTurnLabel;
        gameObject.SetActive(true);

        float offscreenOffset = panelRect.rect.width + slideOvershoot;
        Vector2 hiddenLeft = restPosition + Vector2.left * offscreenOffset;
        Vector2 hiddenRight = restPosition + Vector2.right * offscreenOffset;
        float holdDuration = owner == TurnOwner.Enemy ? enemyHoldDuration : playerHoldDuration;

        panelRect.anchoredPosition = hiddenLeft;

        activeTween = panelRect
            .DOAnchorPos(restPosition, slideInDuration)
            .SetEase(Ease.OutCubic);
        yield return WaitTweenOrSkip(activeTween, shouldSkip);
        activeTween = null;

        if (IsSkipped(shouldSkip))
        {
            ForceHide();
            yield break;
        }

        yield return WaitSecondsOrSkip(holdDuration, shouldSkip);

        if (IsSkipped(shouldSkip))
        {
            ForceHide();
            yield break;
        }

        activeTween = panelRect
            .DOAnchorPos(hiddenRight, slideOutDuration)
            .SetEase(Ease.InCubic);
        yield return WaitTweenOrSkip(activeTween, shouldSkip);
        activeTween = null;

        ForceHide();
    }

    private bool localSkipRequested;

    private static bool IsSkipped(Func<bool> shouldSkip)
    {
        return shouldSkip != null && shouldSkip();
    }

    private bool IsSkippedLocal(Func<bool> shouldSkip)
    {
        return localSkipRequested || IsSkipped(shouldSkip);
    }

    private IEnumerator WaitSecondsOrSkip(float duration, Func<bool> shouldSkip)
    {
        if (duration <= 0f || IsSkippedLocal(shouldSkip))
            yield break;

        float elapsed = 0f;
        while (elapsed < duration && !IsSkippedLocal(shouldSkip))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitTweenOrSkip(Tween tween, Func<bool> shouldSkip)
    {
        while (tween != null && tween.IsActive() && !IsSkippedLocal(shouldSkip))
            yield return null;

        if (IsSkippedLocal(shouldSkip))
            KillActiveTween();
    }

    private void KillActiveTween()
    {
        if (activeTween == null || !activeTween.IsActive())
            return;

        activeTween.Kill();
        activeTween = null;
    }
}
