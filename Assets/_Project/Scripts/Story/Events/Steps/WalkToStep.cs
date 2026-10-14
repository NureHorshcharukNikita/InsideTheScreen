using System;
using System.Collections;
using UnityEngine;

// Walks a character in a straight line (no pathfinding) to a point or to another character.
// It stops facing the way it walked (or, backwards, the other way), and the new position is saved.
// Sideways slides it there in the pose it has, without turning or stepping.
[Serializable, SubclassMenu("Characters/Walk To")]
public sealed class WalkToStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private StoryTarget destination = new(StoryTargetType.SceneObject);
    [Tooltip("Added to the destination, e.g. to stop next to a character instead of on top of them.")]
    [SerializeField] private Vector2 offset;
    [Tooltip("Stops after this far on the way, in units; 0 = goes all the way. E.g. a few steps towards a path, wherever they start.")]
    [SerializeField, Min(0f)] private float maxDistance;
    [SerializeField, Min(0.1f)] private float speed = 2f;
    [Tooltip("Steps back: faces away from where it walks, e.g. backing away from the TV while watching it.")]
    [SerializeField] private bool backwards;
    [Tooltip("Slides over in the pose it has, without turning or stepping, e.g. shuffling aside with the back to the camera.")]
    [SerializeField] private bool sideways;
    [Tooltip("Walks facing up or down whenever it goes up or down at all, also at a slant that is mostly sideways, e.g. walking up to a camp ahead.")]
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

        Vector3 end = target.position + (Vector3)offset;
        end.z = walker.position.z;
        if (maxDistance > 0f && (end - walker.position).magnitude > maxDistance)
            end = walker.position + (end - walker.position).normalized * maxDistance;
        Vector2 facing = end - walker.position;
        if (preferUpDown && Mathf.Abs(facing.y) > 0.01f)
            facing = new Vector2(0f, Mathf.Sign(facing.y));
        if (backwards)
            facing = -facing;
        if (!sideways)
            FlipSpriteToFace.Apply(walker, facing);

        while ((walker.position - end).sqrMagnitude > 0.0001f)
        {
            walker.position = Vector3.MoveTowards(walker.position, end, speed * GameClock.DeltaTime);

            // Every frame: PlayerMovement resets IsMoving while the game is not in Gameplay.
            if (!sideways)
                CharacterWalkAnimation.Apply(animator, true, facing);
            yield return null;
        }

        if (!sideways)
            CharacterWalkAnimation.Apply(animator, false, facing);
        Unmute(footsteps);
        context.SavePosition(walker);
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
