using System;
using System.Collections;
using UnityEngine;

// Fades a character out or back in where they stand, e.g. going into the barn in Leymar, which has
// no inside to walk into, and coming back out of it in the morning. Anyone still faded out shows
// again when the event ends.
[Serializable, SubclassMenu("Characters/Fade Out or In")]
public sealed class FadeCharacterStep : StoryStep
{
    public enum FadeAction
    {
        Out,
        In
    }

    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private FadeAction action = FadeAction.Out;
    [SerializeField, Min(0f)] private float duration = 0.35f;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform character = actor.Resolve(context);
        SpriteRenderer sprite = StoryEventContext.GetSprite(character);
        if (sprite == null)
            yield break;

        // Its colour comes back when the event is released.
        context.TakeControl(character);

        float from = sprite.color.a;
        float to = action == FadeAction.Out ? 0f : 1f;
        for (float elapsed = 0f; elapsed < duration; elapsed += GameClock.DeltaTime)
        {
            SetAlpha(sprite, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetAlpha(sprite, to);
    }

    private static void SetAlpha(SpriteRenderer sprite, float alpha)
    {
        Color color = sprite.color;
        color.a = alpha;
        sprite.color = color;
    }

    public override string ToString()
    {
        return action == FadeAction.Out ? $"{actor} fades out" : $"{actor} fades in";
    }
}
