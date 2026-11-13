using System;
using System.Collections;
using UnityEngine;

// Sends a character walking somewhere while the player plays on: the step is done at once, the event
// ends, and the player can move while the character is still on its way (BackgroundWalk). E.g.
// Hideshi going over to wait by Eldon's tower after the ground lesson. For NPCs, not the player.
[Serializable, SubclassMenu("Characters/Walk Off (Player Keeps Playing)")]
public sealed class WalkOffStep : StoryStep
{
    public enum EndFacing
    {
        WayItWalked,
        Down,
        Up,
        Left,
        Right
    }

    [SerializeField] private StoryTarget actor = new(StoryTargetType.Npc);
    [SerializeField] private StoryTarget destination = new(StoryTargetType.SceneObject);
    [SerializeField, Min(0.1f)] private float speed = 2.2f;
    [Tooltip("Where it looks once it stands there.")]
    [SerializeField] private EndFacing facingAtEnd = EndFacing.WayItWalked;
    [Tooltip("Goes around walls, props and people in the way instead of through them. Straight where nothing is in the way.")]
    [SerializeField] private bool avoidObstacles = true;

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform walker = actor.Resolve(context);
        Transform target = destination.Resolve(context);
        if (walker == null || target == null || walker == context.Player)
            yield break;

        BackgroundWalk.Begin(walker, target.position, speed, FacingDirection, avoidObstacles);
    }

    // Skipped (Debug Test Start): the character is already there, as if it had walked, e.g. Hideshi
    // waiting by Eldon's tower after the ground lesson.
    public override void Skip(StoryEventContext context)
    {
        Transform walker = actor.Resolve(context);
        Transform target = destination.Resolve(context);
        if (walker == null || target == null || walker == context.Player)
            return;

        BackgroundWalk.Place(walker, target.position, FacingDirection);
    }

    private Vector2 FacingDirection => facingAtEnd switch
    {
        EndFacing.Down => Vector2.down,
        EndFacing.Up => Vector2.up,
        EndFacing.Left => Vector2.left,
        EndFacing.Right => Vector2.right,
        _ => Vector2.zero
    };

    public override string ToString()
    {
        return $"{actor} walks off to {destination} (player keeps playing)";
    }
}
