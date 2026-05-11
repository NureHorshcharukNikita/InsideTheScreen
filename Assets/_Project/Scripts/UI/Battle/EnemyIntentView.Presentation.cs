using System.Collections;
using UnityEngine;

public partial class EnemyIntentView
{
    private const float FirstRevealAfterDealDelay = 0.45f;
    private const float RevealFadeInSeconds = 0.12f;
    private const float RevealPulseSeconds = 0.06f;

    private void LateUpdate()
    {
        ResolveFollowRect();
        if (targetCharacter == null || followRectTransform == null || canvasCache == null)
            return;

        SpriteRenderer spriteRenderer = targetCharacter.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        Bounds bounds = spriteRenderer.bounds;
        Vector3 worldPoint = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        Vector3 screenPoint = camera.WorldToScreenPoint(worldPoint);
        if (screenPoint.z <= 0f)
            return;

        Camera uiEventCamera = canvasCache.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera;
        RectTransform canvasRect = canvasCache.transform as RectTransform;
        if (canvasRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiEventCamera, out Vector2 localPoint))
            return;

        float fromPivotToBottom = followRectTransform.pivot.y * followRectTransform.rect.height;
        localPoint.y += fromPivotToBottom + aboveSpritePadding;
        followRectTransform.anchoredPosition = localPoint;
    }

    private void ResolveFollowRect()
    {
        if (intentContainer != null)
        {
            RectTransform containerRect = intentContainer.transform as RectTransform;
            if (containerRect != null)
            {
                followRectTransform = containerRect;
                return;
            }
        }

        followRectTransform = rectTransformCache;
    }

    private void ClearIcon()
    {
        if (intentIcon == null)
            return;

        intentIcon.sprite = null;
        intentIcon.enabled = false;
    }

    private void SetIntentContainerActive(bool active)
    {
        if (intentContainer == null)
            return;

        if (intentContainer == gameObject)
            return;

        intentContainer.SetActive(active);
    }

    private void CaptureBaseColorsIfNeeded()
    {
        if (_capturedBaseColors)
            return;

        if (intentLabel != null)
            _labelBaseColor = intentLabel.color;
        if (intentIcon != null)
            _iconBaseColor = intentIcon.color;
        _capturedBaseColors = true;
    }

    private void SetIntentVisualAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        CaptureBaseColorsIfNeeded();

        if (intentLabel != null)
        {
            Color c = _labelBaseColor;
            c.a = _labelBaseColor.a * alpha;
            intentLabel.color = c;
        }

        if (intentIcon != null)
        {
            Color c = _iconBaseColor;
            c.a = _iconBaseColor.a * alpha;
            intentIcon.color = c;
        }
    }

    private void StopRevealRoutine()
    {
        if (_revealRoutine == null)
            return;

        StopCoroutine(_revealRoutine);
        _revealRoutine = null;
    }

    private void StopBattleStartRevealRoutine()
    {
        if (_battleStartRevealRoutine == null)
            return;

        StopCoroutine(_battleStartRevealRoutine);
        _battleStartRevealRoutine = null;
    }

    private void StopAllRevealCoroutines()
    {
        StopBattleStartRevealRoutine();
        StopRevealRoutine();
    }

    private void StartRevealIntentAnimation()
    {
        StopRevealRoutine();
        _revealRoutine = StartCoroutine(RevealIntentRoutine());
    }

    private IEnumerator HandFlyRevealFallbackRoutine()
    {
        float wait = Mathf.Max(0f, FirstRevealAfterDealDelay);
        float t = 0f;
        while (t < wait)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _battleStartRevealRoutine = null;
        if (_awaitingHandDealFlyReveal)
            NotifyHandDealFlyFinished();
        else
            Refresh();
    }

    private IEnumerator RevealIntentRoutine()
    {
        float t = 0f;
        float fade = Mathf.Max(0.01f, RevealFadeInSeconds);
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            SetIntentVisualAlpha(Mathf.SmoothStep(0f, 1f, t / fade));
            yield return null;
        }

        SetIntentVisualAlpha(1f);

        float pulse = Mathf.Max(0.01f, RevealPulseSeconds);
        t = 0f;
        while (t < pulse)
        {
            t += Time.unscaledDeltaTime;
            float wobble = 1f - 0.12f * Mathf.Sin((t / pulse) * Mathf.PI);
            SetIntentVisualAlpha(wobble);
            yield return null;
        }

        SetIntentVisualAlpha(1f);
        _revealRoutine = null;
    }
}
