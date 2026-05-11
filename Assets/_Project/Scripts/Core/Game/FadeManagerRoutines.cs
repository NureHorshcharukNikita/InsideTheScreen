using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class FadeManager
{
    private IEnumerator FadeToBlackThenLoadRoutine(string sceneName)
    {
        _isFading = true;
        fadeGroup.gameObject.SetActive(true);
        fadeGroup.blocksRaycasts = true;
        fadeGroup.interactable = false;

        yield return FadeAlphaUnscaled(0f, 1f);

        fadeGroup.alpha = 1f;
        s_pendingFadeInAfterLoad = true;
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeInFromBlackRoutine()
    {
        _isFading = true;
        yield return FadeAlphaUnscaled(1f, 0f);
        HideFadeOverlay();
        _isFading = false;
    }

    private IEnumerator FadeAlphaUnscaled(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            fadeGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        fadeGroup.alpha = to;
    }
}
