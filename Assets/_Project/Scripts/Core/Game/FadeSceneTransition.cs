using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class FadeSceneTransition
{
    public static IEnumerator FadeToBlackThenLoad(
        FadeOverlayController overlay,
        float duration,
        string sceneName,
        Action<bool> setFading,
        Action markFadeInAfterLoad)
    {
        setFading?.Invoke(true);
        overlay.ShowForFade();

        yield return FadeAlphaUnscaled(overlay, duration, 0f, 1f);

        overlay.SetAlpha(1f);
        markFadeInAfterLoad?.Invoke();
        SceneManager.LoadScene(sceneName);
    }

    public static IEnumerator FadeInFromBlack(
        FadeOverlayController overlay,
        float duration,
        Action<bool> setFading)
    {
        setFading?.Invoke(true);

        yield return FadeAlphaUnscaled(overlay, duration, 1f, 0f);

        overlay.Hide();
        setFading?.Invoke(false);
    }

    public static IEnumerator FadeToBlackThenAct(
        FadeOverlayController overlay,
        float duration,
        Action onBlack,
        Action<bool> setFading,
        Action onComplete)
    {
        setFading?.Invoke(true);
        overlay.ShowForFade();

        yield return FadeAlphaUnscaled(overlay, duration, 0f, 1f);
        onBlack?.Invoke();
        yield return null;
        yield return FadeAlphaUnscaled(overlay, duration, 1f, 0f);

        overlay.Hide();
        setFading?.Invoke(false);
        onComplete?.Invoke();
    }

    private static IEnumerator FadeAlphaUnscaled(FadeOverlayController overlay, float duration, float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GameClock.DeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            overlay.SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        overlay.SetAlpha(to);
    }
}
