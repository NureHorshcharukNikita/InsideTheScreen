using System;
using System.Collections;
using UnityEngine;

internal sealed class EnemyIntentRevealAnimator
{
    private const float FirstRevealAfterDealDelay = 0.45f;
    private const float RevealFadeInSeconds = 0.12f;
    private const float RevealPulseSeconds = 0.06f;

    private readonly MonoBehaviour coroutineOwner;
    private readonly Action<float> setVisualAlpha;
    private readonly Action fallbackElapsed;

    private Coroutine revealRoutine;
    private Coroutine fallbackRoutine;

    public EnemyIntentRevealAnimator(MonoBehaviour coroutineOwner, Action<float> setVisualAlpha, Action fallbackElapsed)
    {
        this.coroutineOwner = coroutineOwner;
        this.setVisualAlpha = setVisualAlpha;
        this.fallbackElapsed = fallbackElapsed;
    }

    public void StopReveal()
    {
        if (revealRoutine == null)
            return;

        coroutineOwner.StopCoroutine(revealRoutine);
        revealRoutine = null;
    }

    public void StopFallback()
    {
        if (fallbackRoutine == null)
            return;

        coroutineOwner.StopCoroutine(fallbackRoutine);
        fallbackRoutine = null;
    }

    public void StopAll()
    {
        StopFallback();
        StopReveal();
    }

    public void StartReveal()
    {
        StopReveal();
        revealRoutine = coroutineOwner.StartCoroutine(RevealIntentRoutine());
    }

    public void StartFallback()
    {
        StopFallback();
        fallbackRoutine = coroutineOwner.StartCoroutine(HandFlyRevealFallbackRoutine());
    }

    private IEnumerator HandFlyRevealFallbackRoutine()
    {
        float wait = Mathf.Max(0f, FirstRevealAfterDealDelay);
        float elapsed = 0f;
        while (elapsed < wait)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        fallbackRoutine = null;
        fallbackElapsed?.Invoke();
    }

    private IEnumerator RevealIntentRoutine()
    {
        float elapsed = 0f;
        float fade = Mathf.Max(0.01f, RevealFadeInSeconds);
        while (elapsed < fade)
        {
            elapsed += Time.unscaledDeltaTime;
            setVisualAlpha(Mathf.SmoothStep(0f, 1f, elapsed / fade));
            yield return null;
        }

        setVisualAlpha(1f);

        float pulse = Mathf.Max(0.01f, RevealPulseSeconds);
        elapsed = 0f;
        while (elapsed < pulse)
        {
            elapsed += Time.unscaledDeltaTime;
            float wobble = 1f - 0.12f * Mathf.Sin((elapsed / pulse) * Mathf.PI);
            setVisualAlpha(wobble);
            yield return null;
        }

        setVisualAlpha(1f);
        revealRoutine = null;
    }
}
