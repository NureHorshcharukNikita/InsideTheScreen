using System;
using System.Collections;
using UnityEngine;

// A character looks around where they stand: a glance one way, then the other (left and right, or
// up and down), e.g. stopping on a forest path to listen. At the end they can face something,
// e.g. the way on.
[Serializable, SubclassMenu("Characters/Look Around")]
public sealed class LookAroundStep : StoryStep
{
    public enum LookAxis
    {
        LeftAndRight,
        UpAndDown
    }

    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private LookAxis axis = LookAxis.LeftAndRight;
    [Tooltip("How many glances, alternating one way and the other.")]
    [SerializeField, Min(1)] private int glances = 2;
    [Tooltip("Starts the other way: right instead of left, or down instead of up, e.g. so two characters do not turn in step.")]
    [SerializeField] private bool otherWayFirst;
    [Tooltip("How long each glance lasts.")]
    [SerializeField, Min(0.05f)] private float interval = 0.45f;
    [Tooltip("Faces it at the end, e.g. the way on. Empty = stays as the last glance.")]
    [SerializeField] private Transform faceAtEnd;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform body = actor.Resolve(context);
        if (body == null)
            yield break;

        context.TakeControl(body);
        Vector2 firstWay = axis == LookAxis.LeftAndRight ? Vector2.left : Vector2.up;
        for (int i = 0; i < glances; i++)
        {
            bool firstWayNow = (i % 2 == 0) != otherWayFirst;
            Face(body, firstWayNow ? firstWay : -firstWay);
            yield return new WaitForGameSeconds(interval);
        }

        if (faceAtEnd != null)
            Face(body, faceAtEnd.position - body.position);
    }

    private static void Face(Transform body, Vector2 direction)
    {
        CharacterWalkAnimation.Apply(StoryEventContext.GetAnimator(body), false, direction);
        FlipSpriteToFace.Apply(body, direction);
    }

    public override string ToString()
    {
        return $"{actor} looks around";
    }
}
