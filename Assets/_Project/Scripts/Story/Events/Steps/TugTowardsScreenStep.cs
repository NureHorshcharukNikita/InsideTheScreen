using System;
using System.Collections;
using UnityEngine;

// The screen starts to tug at a character before pulling them in: they slide a little towards
// it, take on some of the noise's colour and keep trembling until a Pull Into Screen step takes
// over or the event ends. A later Tug step can pull harder.
[Serializable, SubclassMenu("Effects/Tug Towards Screen")]
public sealed class TugTowardsScreenStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private Transform screen;
    [Tooltip("How far towards the screen, as a part of the way there from where the first tug found them.")]
    [SerializeField, Range(0f, 1f)] private float distance = 0.05f;
    [Tooltip("How much of the noise's colour the character takes on.")]
    [SerializeField, Range(0f, 1f)] private float tint = 0.15f;
    [Tooltip("How hard they tremble, in units.")]
    [SerializeField, Min(0f)] private float shake = 0.02f;
    [SerializeField, Min(0f)] private float duration = 1f;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform target = actor.Resolve(context);
        if (target == null || screen == null)
            yield break;

        context.TakeControl(target);
        ScreenTug tug = ScreenTug.Get(context, target);
        SpriteRenderer sprite = StoryEventContext.GetSprite(target);
        ScreenNoiseOverlay screenNoise = context.GetEffect<ScreenNoiseOverlay>();
        Color noiseColor = screenNoise != null ? screenNoise.Tint : Color.white;

        Vector3 toScreen = screen.position - tug.Origin;
        toScreen.z = 0f;
        Vector3 fromAnchor = tug.Anchor;
        Vector3 toAnchor = tug.Origin + toScreen * distance;
        float fromShake = tug.Shake;
        Color fromColor = sprite != null ? sprite.color : Color.white;
        Color toColor = Color.Lerp(tug.BaseColor, noiseColor, tint);
        toColor.a = tug.BaseColor.a;

        for (float elapsed = 0f; elapsed < duration; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / duration;
            float eased = t * t * (3f - 2f * t);
            tug.Anchor = Vector3.Lerp(fromAnchor, toAnchor, eased);
            tug.Shake = Mathf.Lerp(fromShake, shake, t);
            if (sprite != null)
                sprite.color = Color.Lerp(fromColor, toColor, eased);
            yield return null;
        }

        tug.Anchor = toAnchor;
        tug.Shake = shake;
        if (sprite != null)
            sprite.color = toColor;
    }

    public override string ToString()
    {
        return $"Screen tugs at {actor}";
    }
}
