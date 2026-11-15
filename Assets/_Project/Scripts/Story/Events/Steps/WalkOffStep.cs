using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sends a character walking somewhere while the player plays on: the step is done at once, the event
// ends, and the player can move while the character is still on its way (BackgroundWalk). E.g.
// Hideshi going over to wait by Eldon's tower after the ground lesson. For NPCs, not the player.
// It can come out of somewhere as it sets off, e.g. Garr out of his barn, and call an event (Start:
// When Called) when the player gets in its way, e.g. Garr telling Haru off for it.
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
    [Tooltip("Points it walks through first, in order, e.g. along the roads instead of across the grass right behind a house. Empty = straight there (around what is in the way).")]
    [SerializeField] private List<Transform> via = new();
    [SerializeField, Min(0.1f)] private float speed = 2.2f;
    [Tooltip("Where it looks once it stands there.")]
    [SerializeField] private EndFacing facingAtEnd = EndFacing.WayItWalked;
    [Tooltip("Goes around walls, props and people in the way instead of through them. Straight where nothing is in the way.")]
    [SerializeField] private bool avoidObstacles = true;
    [Tooltip("It comes out of somewhere as it sets off: out of sight until then, then fading in where it stands, e.g. Garr out of his barn.")]
    [SerializeField] private bool comesOut;
    [Tooltip("Comes Out: heard as it comes out, if it is on screen, e.g. the barn door. Empty = nothing.")]
    [SerializeField] private AudioClip comeOutSound;
    [SerializeField, Range(0f, 1f)] private float comeOutVolume = 0.6f;
    [Tooltip("Played when the player gets in its way on the way there (its Start: When Called), e.g. Garr telling Haru off; it stands still meanwhile and walks on after. Once that event is done, or while its conditions are not met, it just walks through. Empty = none.")]
    [SerializeField] private StoryEvent whenInTheWay;
    [Tooltip("It goes on walking while a dialogue with someone is open (not in a cutscene, a menu or the pause), unless it is the one talking, e.g. Garr coming out of his barn while Haru talks to his father. Walking into the player meanwhile it waits in front of him, and When In The Way plays after the talk. Off = it stands while a dialogue or a menu is open.")]
    [SerializeField] private bool walksDuringTalks;

    public override bool IsInstant => true;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform walker = actor.Resolve(context);
        Transform target = destination.Resolve(context);
        if (walker == null || target == null || walker == context.Player)
            yield break;

        var points = new List<Vector2>();
        foreach (Transform point in via)
        {
            if (point != null)
                points.Add(point.position);
        }

        BackgroundWalk walk = BackgroundWalk.Begin(walker, target.position, speed, FacingDirection, avoidObstacles, walksDuringTalks, points);
        if (walk == null)
            yield break;

        if (comesOut)
            walk.ComeOut(comeOutSound, comeOutVolume);
        if (whenInTheWay != null)
            walk.CallWhenInTheWay(whenInTheWay);
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
