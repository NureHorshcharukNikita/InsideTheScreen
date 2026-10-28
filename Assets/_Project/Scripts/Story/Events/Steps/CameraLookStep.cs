using System;
using System.Collections;
using UnityEngine;

// The camera glides over to a character or a point and stays on it, e.g. to show who the
// characters are talking about before they walk there. Another Camera Look At with the player
// brings it back. When the event ends the camera follows the player again in any case.
// Needs Camera Follows on the event: the camera stands still otherwise.
[Serializable, SubclassMenu("Camera/Look At")]
public sealed class CameraLookStep : StoryStep
{
    [Tooltip("What the camera shows. Player = back to the player.")]
    [SerializeField] private StoryTarget target = new(StoryTargetType.Npc);
    [Tooltip("Added to the target's position, e.g. to show the house behind someone too.")]
    [SerializeField] private Vector2 offset;
    [Tooltip("How long the camera takes to get there.")]
    [SerializeField, Min(0f)] private float seconds = 1.5f;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform shown = target.Resolve(context);
        if (shown == null)
            yield break;

        StoryCameraFocus focus = context.GetEffect<StoryCameraFocus>();
        if (focus == null)
        {
            focus = StoryCameraFocus.TakeMainCamera();
            if (focus == null)
                yield break;

            context.AddEffect(focus);
        }

        Vector3 from = focus.Position;
        for (float time = 0f; time < seconds; time += GameClock.DeltaTime)
        {
            if (shown == null)
                yield break;

            focus.MoveTo(Vector3.Lerp(from, shown.position + (Vector3)offset, Mathf.SmoothStep(0f, 1f, time / seconds)));
            yield return null;
        }

        if (shown != null)
            focus.Stay(shown, offset);
    }

    public override string ToString()
    {
        return $"Camera looks at {target} ({seconds:0.##} s)";
    }
}
