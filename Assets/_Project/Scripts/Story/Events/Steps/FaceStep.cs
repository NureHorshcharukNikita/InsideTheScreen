using System;
using System.Collections;
using UnityEngine;

// Turns a character to a direction or towards another character, without moving.
[Serializable, SubclassMenu("Characters/Face")]
public sealed class FaceStep : StoryStep
{
    public enum Facing
    {
        Down,
        Up,
        Left,
        Right,
        TowardsTarget
    }

    [SerializeField] private StoryTarget actor = new();
    [SerializeField] private Facing facing = Facing.Down;
    [Tooltip("Only used with Towards Target.")]
    [SerializeField] private StoryTarget target = new(StoryTargetType.Npc);

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform turner = actor.Resolve(context);
        if (turner == null)
            yield break;

        Vector2 direction = GetDirection(context, turner);
        if (direction == Vector2.zero)
            yield break;

        context.TakeControl(turner);
        CharacterWalkAnimation.Apply(StoryEventContext.GetAnimator(turner), false, direction);
        FlipSpriteToFace.Apply(turner, direction);
    }

    private Vector2 GetDirection(StoryEventContext context, Transform turner)
    {
        switch (facing)
        {
            case Facing.Up:
                return Vector2.up;
            case Facing.Left:
                return Vector2.left;
            case Facing.Right:
                return Vector2.right;
            case Facing.TowardsTarget:
                Transform other = target.Resolve(context);
                return other != null ? (Vector2)(other.position - turner.position) : Vector2.zero;
            default:
                return Vector2.down;
        }
    }

    public override string ToString()
    {
        return facing == Facing.TowardsTarget
            ? $"{actor} faces {target}"
            : $"{actor} faces {facing}";
    }
}
