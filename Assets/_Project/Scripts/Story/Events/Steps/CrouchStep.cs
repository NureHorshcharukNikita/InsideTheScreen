using System;
using System.Collections;
using UnityEngine;

// Crouches a character or stands them back up, e.g. Hideshi putting the console down: crouch,
// show the console, stand up. Crouching squashes the sprite down onto the feet. Anyone still
// crouching stands up when the event ends.
[Serializable, SubclassMenu("Characters/Crouch or Stand Up")]
public sealed class CrouchStep : StoryStep
{
    public enum CrouchAction
    {
        Crouch,
        StandUp
    }

    // Lower and a little wider, as if bending the knees.
    private static readonly Vector3 Squash = new(1.06f, 0.86f, 1f);

    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private CrouchAction action = CrouchAction.Crouch;
    [SerializeField, Min(0.01f)] private float duration = 0.18f;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform body = actor.Resolve(context);
        if (body == null)
            yield break;

        context.TakeControl(body);

        Vector3 from = body.localScale;
        Vector3 to = action == CrouchAction.Crouch
            ? Vector3.Scale(from, Squash)
            : new Vector3(from.x / Squash.x, from.y / Squash.y, from.z / Squash.z);
        if (action == CrouchAction.Crouch)
            context.OnRelease(() => context.RestoreLook(body));

        for (float elapsed = 0f; elapsed < duration; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / duration;
            body.localScale = Vector3.Lerp(from, to, t * (2f - t));
            yield return null;
        }

        body.localScale = to;
    }

    public override string ToString()
    {
        return action == CrouchAction.Crouch ? $"{actor} crouches" : $"{actor} stands up";
    }
}
