using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

// Eyes opening from a black screen, e.g. waking up in the forest: an eye-shaped opening in the
// black blinks a few times and opens, and the blurred view sharpens as the eyes open wider.
// Put it after a Fade to black: it starts closed and takes over from the fade.
[Serializable, SubclassMenu("Effects/Open Eyes")]
public sealed class OpenEyesStep : StoryStep
{
    // Up to this openness everything is blurred; from there it sharpens until the eyes are open.
    private const float BlurredUntil = 0.3f;

    [Tooltip("How open the eyes are (0 = closed, 1 = open) over time, in seconds. The step lasts until the last key.")]
    [SerializeField] private AnimationCurve openness = new(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 0.3f),
        new Keyframe(0.9f, 0.3f),
        new Keyframe(1.2f, 0f),
        new Keyframe(2.2f, 0f),
        new Keyframe(2.8f, 0.55f),
        new Keyframe(3f, 0.2f),
        new Keyframe(3.8f, 1f));

    public override IEnumerator Run(StoryEventContext context)
    {
        EyeLidsOverlay lids = EyeLidsOverlay.Create();
        context.AddEffect(lids);

        // The closed lids cover the screen now: the black of the fade is not needed under them.
        CanvasGroup fade = FadeManager.Instance != null ? FadeManager.Instance.FadeOverlayGroup : null;
        new FadeOverlayController(fade).Hide();

        float length = openness.length > 0 ? openness[openness.length - 1].time : 0f;
        float widest = 0f;
        for (float elapsed = 0f; elapsed < length; elapsed += GameClock.DeltaTime)
        {
            float open = Mathf.Clamp01(openness.Evaluate(elapsed));
            // The view sharpens as the eyes open wider and stays so when they blink. A first peek
            // through barely open eyes is all blur.
            widest = Mathf.Max(widest, open);
            float focus = Mathf.InverseLerp(BlurredUntil, 1f, widest);
            lids.SetOpenness(open, 1f - Mathf.SmoothStep(0f, 1f, focus));
            yield return null;
        }

        Object.Destroy(lids.gameObject);
    }

    public override string ToString()
    {
        return "Open eyes";
    }
}
