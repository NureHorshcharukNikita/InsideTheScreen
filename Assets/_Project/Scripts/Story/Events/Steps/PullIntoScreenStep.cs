using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

// The screen pulls a character in: a short lean towards it, shaking as if resisting, then a
// fast stretch into the glow while the sprite takes the noise's colour and fades out.
// The character stays hidden until a Restore Look step or the end of the event.
[Serializable, SubclassMenu("Effects/Pull Into Screen")]
public sealed class PullIntoScreenStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private Transform screen;
    [SerializeField, Min(0f)] private float leanDuration = 0.5f;
    [SerializeField, Min(0.1f)] private float pullDuration = 0.8f;
    [Tooltip("Strength the Screen Noise reaches while the character is pulled in.")]
    [SerializeField, Range(0f, 1f)] private float noiseAtEnd = 1f;
    [SerializeField, Range(0f, 1f)] private float glowAtEnd = 1f;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform target = actor.Resolve(context);
        if (target == null || screen == null)
            yield break;

        context.TakeControl(target);
        SpriteRenderer sprite = StoryEventContext.GetSprite(target);
        ScreenNoiseOverlay screenNoise = context.GetEffect<ScreenNoiseOverlay>();
        Color tint = screenNoise != null ? screenNoise.Tint : Color.white;

        Vector3 startPosition = target.position;
        Vector3 endPosition = screen.position;
        endPosition.z = startPosition.z;
        Vector3 leanPosition = Vector3.Lerp(startPosition, endPosition, 0.15f);
        Vector3 startScale = target.localScale;
        Color startColor = sprite != null ? sprite.color : Color.white;

        for (float elapsed = 0f; elapsed < leanDuration; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / leanDuration;
            target.position = Vector3.Lerp(startPosition, leanPosition, t * t) + Jitter(0.04f);
            Tint(sprite, startColor, tint, t * 0.3f, 1f);
            yield return null;
        }

        if (screenNoise != null)
        {
            screenNoise.SetNoise(noiseAtEnd, pullDuration);
            screenNoise.SetGlow(glowAtEnd, pullDuration);
            screenNoise.Whoosh(pullDuration);
        }

        for (float elapsed = 0f; elapsed < pullDuration; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / pullDuration;
            float eased = t * t * t;
            target.position = Vector3.Lerp(leanPosition, endPosition, eased) + Jitter(0.05f * (1f - t));

            // Stretched towards the screen: thinner and taller first, then gone.
            float width = Mathf.Lerp(1f, 0.08f, eased);
            float height = Mathf.Lerp(1f, 1.4f, t) * (1f - eased);
            target.localScale = new Vector3(startScale.x * width, startScale.y * height, startScale.z);
            Tint(sprite, startColor, tint, Mathf.Lerp(0.3f, 1f, t), 1f - eased);
            yield return null;
        }

        target.position = endPosition;
        target.localScale = Vector3.zero;
    }

    private static void Tint(SpriteRenderer sprite, Color baseColor, Color tint, float amount, float alpha)
    {
        if (sprite == null)
            return;

        Color color = Color.Lerp(baseColor, tint, amount);
        color.a = baseColor.a * alpha;
        sprite.color = color;
    }

    private static Vector3 Jitter(float strength)
    {
        return new Vector3(Random.Range(-strength, strength), 0f, 0f);
    }

    public override string ToString()
    {
        return $"{actor} is pulled into the screen";
    }
}
