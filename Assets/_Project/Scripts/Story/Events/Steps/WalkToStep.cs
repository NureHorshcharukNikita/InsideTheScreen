using System;
using System.Collections;
using UnityEngine;

// Walks a character (no pathfinding) to a point or to another character: in a straight line, or
// first across and then up or down (or the other way round, or up/down-across-up/down), turning
// at the corners.
// It stops facing the way it walked (or, backwards, the other way), and the new position is saved.
// Sideways slides it there in the pose it has, without turning or stepping.
[Serializable, SubclassMenu("Characters/Walk To")]
public sealed class WalkToStep : StoryStep
{
    public enum WalkRoute
    {
        Straight,
        AcrossThenUpDown,
        UpDownThenAcross,
        UpDownAcrossUpDown
    }

    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private StoryTarget destination = new(StoryTargetType.SceneObject);
    [Tooltip("Added to the destination, e.g. to stop next to a character instead of on top of them.")]
    [SerializeField] private Vector2 offset;
    [Tooltip("Straight: the shortest way, at a slant if needed. Across Then Up Down: first left or right, then up or down, e.g. lining up below someone and walking up to them. Up Down Then Across: the other way round. Up Down Across Up Down: a little up or down first (out of the bushes onto the path), then across, then the last stretch up or down.")]
    [SerializeField] private WalkRoute route = WalkRoute.Straight;
    [Tooltip("Up Down Across Up Down only: how far it walks up or down at the end, after walking across, in units. Larger = turns across sooner.")]
    [SerializeField, Min(0f)] private float lastUpDown = 2f;
    [Tooltip("Stops after this far on the way, in units; 0 = goes all the way. E.g. a few steps towards a path, wherever they start.")]
    [SerializeField, Min(0f)] private float maxDistance;
    [SerializeField, Min(0.1f)] private float speed = 2f;
    [Tooltip("Steps back: faces away from where it walks, e.g. backing away from the TV while watching it.")]
    [SerializeField] private bool backwards;
    [Tooltip("Slides over in the pose it has, without turning or stepping, e.g. shuffling aside with the back to the camera.")]
    [SerializeField] private bool sideways;
    [Tooltip("Straight route only: walks facing up or down whenever it goes up or down at all, also at a slant that is mostly sideways, e.g. walking up to a camp ahead.")]
    [SerializeField] private bool preferUpDown;
    [Tooltip("No footsteps, e.g. walking up outside, out of view, before coming in.")]
    [SerializeField] private bool silent;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform walker = actor.Resolve(context);
        Transform target = destination.Resolve(context);
        if (walker == null || target == null)
            yield break;

        context.TakeControl(walker);
        Animator animator = StoryEventContext.GetAnimator(walker);
        FootstepAudio footsteps = silent ? walker.GetComponentInChildren<FootstepAudio>() : null;
        if (footsteps != null)
        {
            footsteps.Muted = true;
            // Heard again also if the event stops halfway.
            context.OnRelease(() => Unmute(footsteps));
        }

        Vector3 start = walker.position;
        Vector3 end = target.position + (Vector3)offset;
        end.z = start.z;
        if (maxDistance > 0f && (end - start).magnitude > maxDistance)
            end = start + (end - start).normalized * maxDistance;

        float upDown = end.y - start.y;
        float acrossY = end.y - Mathf.Sign(upDown) * Mathf.Min(lastUpDown, Mathf.Abs(upDown));
        Vector3[] points = route switch
        {
            WalkRoute.AcrossThenUpDown => new[] { new Vector3(end.x, start.y, start.z), end },
            WalkRoute.UpDownThenAcross => new[] { new Vector3(start.x, end.y, start.z), end },
            WalkRoute.UpDownAcrossUpDown => new[]
            {
                new Vector3(start.x, acrossY, start.z),
                new Vector3(end.x, acrossY, start.z),
                end
            },
            _ => new[] { end }
        };

        Vector2 facing = FacingTowards(end - start);
        foreach (Vector3 point in points)
        {
            if ((walker.position - point).sqrMagnitude <= 0.0001f)
                continue;

            facing = FacingTowards(point - walker.position);
            if (!sideways)
                FlipSpriteToFace.Apply(walker, facing);

            while ((walker.position - point).sqrMagnitude > 0.0001f)
            {
                walker.position = Vector3.MoveTowards(walker.position, point, speed * GameClock.DeltaTime);

                // Every frame: PlayerMovement resets IsMoving while the game is not in Gameplay.
                if (!sideways)
                    CharacterWalkAnimation.Apply(animator, true, facing);
                yield return null;
            }
        }

        if (!sideways)
            CharacterWalkAnimation.Apply(animator, false, facing);
        Unmute(footsteps);
        context.SavePosition(walker);
    }

    private Vector2 FacingTowards(Vector2 direction)
    {
        if (preferUpDown && Mathf.Abs(direction.y) > 0.01f)
            direction = new Vector2(0f, Mathf.Sign(direction.y));

        return backwards ? -direction : direction;
    }

    private static void Unmute(FootstepAudio footsteps)
    {
        if (footsteps != null)
            footsteps.Muted = false;
    }

    public override string ToString()
    {
        string quietly = silent ? " quietly" : string.Empty;
        if (sideways)
            return $"{actor} slides{quietly} to {destination}";

        return backwards
            ? $"{actor} backs away{quietly} to {destination}"
            : $"{actor} walks{quietly} to {destination}";
    }
}
