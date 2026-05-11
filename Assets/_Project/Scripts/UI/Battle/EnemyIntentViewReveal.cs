using System.Collections;
using UnityEngine;

public partial class EnemyIntentView
{
    private const float FirstRevealAfterDealDelay = 0.45f;
    private const float RevealFadeInSeconds = 0.12f;
    private const float RevealPulseSeconds = 0.06f;

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
