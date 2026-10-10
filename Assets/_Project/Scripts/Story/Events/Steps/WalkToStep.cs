using System;
using System.Collections;
using UnityEngine;

// Walks a character in a straight line (no pathfinding) to a point or to another character.
// It stops facing the way it walked, and the new position is saved.
[Serializable, SubclassMenu("Characters/Walk To")]
public sealed class WalkToStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private StoryTarget destination = new(StoryTargetType.SceneObject);
    [Tooltip("Added to the destination, e.g. to stop next to a character instead of on top of them.")]
    [SerializeField] private Vector2 offset;
    [SerializeField, Min(0.1f)] private float speed = 2f;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform walker = actor.Resolve(context);
        Transform target = destination.Resolve(context);
        if (walker == null || target == null)
            yield break;

        context.TakeControl(walker);
        Animator animator = StoryEventContext.GetAnimator(walker);

        Vector3 end = target.position + (Vector3)offset;
        end.z = walker.position.z;
        Vector2 direction = end - walker.position;

        while ((walker.position - end).sqrMagnitude > 0.0001f)
        {
            walker.position = Vector3.MoveTowards(walker.position, end, speed * Time.unscaledDeltaTime);

            // Every frame: PlayerMovement resets IsMoving while the game is not in Gameplay.
            CharacterWalkAnimation.Apply(animator, true, direction);
            yield return null;
        }

        CharacterWalkAnimation.Apply(animator, false, direction);
        context.SavePosition(walker);
    }

    public override string ToString()
    {
        return $"{actor} walks to {destination}";
    }
}
