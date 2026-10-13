using System;
using System.Collections;
using UnityEngine;

// Fades the screen to black or back, with the same overlay as scene transitions. An event
// that ends while the screen is still black clears it at the end.
[Serializable, SubclassMenu("Scene/Fade")]
public sealed class FadeStep : StoryStep
{
    public enum FadeDirection
    {
        ToBlack,
        FromBlack
    }

    [SerializeField] private FadeDirection direction = FadeDirection.ToBlack;
    [SerializeField, Min(0f)] private float duration = 0.5f;

    public override IEnumerator Run(StoryEventContext context)
    {
        CanvasGroup group = FadeManager.Instance != null ? FadeManager.Instance.FadeOverlayGroup : null;
        if (group == null)
            yield break;

        FadeOverlayController overlay = new(group);
        bool toBlack = direction == FadeDirection.ToBlack;
        float from = group.gameObject.activeSelf ? group.alpha : 0f;
        float to = toBlack ? 1f : 0f;

        overlay.ShowForFade();
        for (float elapsed = 0f; elapsed < duration; elapsed += GameClock.DeltaTime)
        {
            overlay.SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        if (toBlack)
        {
            overlay.SetAlpha(1f);
            context.OnRelease(overlay.Hide);
        }
        else
        {
            overlay.Hide();
        }
    }

    public override string ToString()
    {
        return $"{(direction == FadeDirection.ToBlack ? "Fade to black" : "Fade from black")} ({duration:0.##} s)";
    }
}
