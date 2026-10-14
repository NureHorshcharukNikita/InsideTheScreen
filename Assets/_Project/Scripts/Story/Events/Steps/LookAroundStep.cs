using System;
using System.Collections;
using UnityEngine;

// A character looks around where they stand: a glance one way, then the other, e.g. stopping on a
// forest path to listen. At the end they can face something, e.g. the way on.
[Serializable, SubclassMenu("Characters/Look Around")]
public sealed class LookAroundStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new();
    [Tooltip("How many glances, alternating left and right.")]
    [SerializeField, Min(1)] private int glances = 2;
    [Tooltip("Starts with a glance to the right instead of the left, e.g. so two characters do not turn in step.")]
    [SerializeField] private bool startRight;
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
        for (int i = 0; i < glances; i++)
        {
            bool right = (i % 2 == 0) == startRight;
            Face(body, right ? Vector2.right : Vector2.left);
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
