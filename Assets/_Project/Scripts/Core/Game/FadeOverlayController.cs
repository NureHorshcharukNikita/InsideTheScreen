using UnityEngine;

internal sealed class FadeOverlayController
{
    private readonly CanvasGroup fadeGroup;

    public FadeOverlayController(CanvasGroup fadeGroup)
    {
        this.fadeGroup = fadeGroup;
    }

    public bool IsAvailable => fadeGroup != null;

    public void ShowBlack()
    {
        if (fadeGroup == null)
            return;

        fadeGroup.gameObject.SetActive(true);
        fadeGroup.alpha = 1f;
        fadeGroup.blocksRaycasts = true;
        fadeGroup.interactable = false;
    }

    public void ShowForFade()
    {
        if (fadeGroup == null)
            return;

        fadeGroup.gameObject.SetActive(true);
        fadeGroup.blocksRaycasts = true;
        fadeGroup.interactable = false;
    }

    public void Hide()
    {
        if (fadeGroup == null)
            return;

        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;
        fadeGroup.gameObject.SetActive(false);
    }

    public void SetAlpha(float alpha)
    {
        if (fadeGroup != null)
            fadeGroup.alpha = alpha;
    }
}
