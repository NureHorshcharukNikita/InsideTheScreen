using System;
using System.Collections;
using UnityEngine;

// Lays a character on the ground or gets them back up, e.g. waking up in the forest after the TV
// pulled them in. Lying is the front-facing sprite turned on its side around the feet; getting up
// turns it back with a little spring. Anyone still lying stands up when the event ends.
[Serializable, SubclassMenu("Characters/Lie Down or Get Up")]
public sealed class LieDownStep : StoryStep
{
    public enum LieAction
    {
        LieDown,
        GetUp
    }

    public enum HeadSide
    {
        Left,
        Right
    }

    private const float SpringHeight = 0.15f;

    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private LieAction action = LieAction.LieDown;
    [Tooltip("Where the head points while lying. Only used when lying down.")]
    [SerializeField] private HeadSide head = HeadSide.Left;
    [Tooltip("Only used when getting up.")]
    [SerializeField, Min(0.05f)] private float getUpDuration = 0.45f;

    public override bool IsInstant => action == LieAction.LieDown;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform body = actor.Resolve(context);
        if (body == null)
            yield break;

        context.TakeControl(body);

        if (action == LieAction.LieDown)
        {
            CharacterWalkAnimation.Apply(StoryEventContext.GetAnimator(body), false, Vector2.down);
            body.localRotation = Quaternion.Euler(0f, 0f, head == HeadSide.Left ? 90f : -90f);
            context.OnRelease(() => StandUp(body));
            yield break;
        }

        Quaternion lying = body.localRotation;
        Vector3 ground = body.position;
        for (float elapsed = 0f; elapsed < getUpDuration; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / getUpDuration;
            body.localRotation = Quaternion.Slerp(lying, Quaternion.identity, 1f - (1f - t) * (1f - t));
            body.position = ground + Vector3.up * (Mathf.Sin(t * Mathf.PI) * SpringHeight);
            yield return null;
        }

        body.position = ground;
        StandUp(body);
    }

    private static void StandUp(Transform body)
    {
        if (body != null)
            body.localRotation = Quaternion.identity;
    }

    public override string ToString()
    {
        return action == LieAction.LieDown ? $"{actor} lies down" : $"{actor} gets up";
    }
}
