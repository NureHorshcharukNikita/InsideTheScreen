using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A character leads the way through set points, turning at each, and someone can follow along the
// same way, e.g. Hideshi taking Haru from the tower to the apple bushes. From where the leader
// stands to the first point it goes across, then up or down (or the other way round), so it never
// walks at a slant: put the points at the corners of the way. The follower first goes to where the
// leader started, the same way, then walks the leader's way, keeping a gap behind, and stops that
// gap short of where the leader stops. Both stop facing the way they walked, and the new positions
// are saved.
[Serializable, SubclassMenu("Characters/Walk A Path")]
public sealed class WalkPathStep : StoryStep
{
    [SerializeField] private StoryTarget actor = new(StoryTargetType.Npc);
    [Tooltip("The corners of the way, in order. The last one is where the leader stops.")]
    [SerializeField] private List<Transform> points = new();
    [Tooltip("To the first point: up or down first, then across. Off: across first.")]
    [SerializeField] private bool upDownFirst;
    [SerializeField, Min(0.1f)] private float speed = 2.5f;
    [Tooltip("Someone walks behind along the same way, e.g. Haru after his father.")]
    [SerializeField] private bool withFollower;
    [SerializeField] private StoryTarget follower = new();
    [Tooltip("How far behind the follower keeps, in units.")]
    [SerializeField, Min(0.5f)] private float followGap = 1.8f;

    private sealed class Walker
    {
        private readonly Transform body;
        private readonly Animator animator;
        private readonly List<Vector3> way;
        private int next;
        private Vector2 facing;

        public Walker(StoryEventContext context, Transform body, List<Vector3> way)
        {
            this.body = body;
            this.way = way;
            context.TakeControl(body);
            animator = StoryEventContext.GetAnimator(body);

            // Waiting to set off, it already looks the way it will go.
            facing = Vector2.down;
            foreach (Vector3 point in way)
            {
                Vector2 toPoint = point - body.position;
                if (toPoint.sqrMagnitude > 0.0001f)
                {
                    facing = toPoint.normalized;
                    FlipSpriteToFace.Apply(body, facing);
                    break;
                }
            }
        }

        public bool Done => next >= way.Count;

        // How far it still has to go along its way.
        public float Left
        {
            get
            {
                float left = 0f;
                Vector2 from = body.position;
                for (int i = next; i < way.Count; i++)
                {
                    left += Vector2.Distance(from, way[i]);
                    from = way[i];
                }

                return left;
            }
        }

        // Up to this far along the way; 0 stands still, in the walking pose of its last step. Goes on
        // to the next point only once this one is reached: what is left of a step after a corner can
        // be smaller than a float can move at these coordinates, and would never run out.
        public void Advance(float distance)
        {
            bool moved = false;
            while (distance > 0f && !Done)
            {
                Vector3 position = body.position;
                Vector3 point = way[next];
                point.z = position.z;
                Vector2 toPoint = point - position;
                if (toPoint.sqrMagnitude <= 0.0001f)
                {
                    next++;
                    continue;
                }

                if ((toPoint.normalized - facing).sqrMagnitude > 0.0001f)
                {
                    facing = toPoint.normalized;
                    FlipSpriteToFace.Apply(body, facing);
                }

                moved = true;
                float left = toPoint.magnitude;
                if (distance < left)
                {
                    body.position = Vector3.MoveTowards(position, point, distance);
                    break;
                }

                body.position = point;
                distance -= left;
                next++;
            }

            // Every frame: PlayerMovement resets IsMoving while the game is not in Gameplay.
            CharacterWalkAnimation.Apply(animator, moved, facing);
        }

        public void Stop(StoryEventContext context)
        {
            CharacterWalkAnimation.Apply(animator, false, facing);
            context.SavePosition(body);
        }
    }

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform leaderBody = actor.Resolve(context);
        if (leaderBody == null)
            yield break;

        List<Vector3> leaderWay = WayFrom(leaderBody.position);
        if (leaderWay.Count == 0)
            yield break;

        Transform followerBody = withFollower ? follower.Resolve(context) : null;
        Walker leader = new(context, leaderBody, leaderWay);
        Walker behind = null;
        if (followerBody != null && followerBody != leaderBody)
        {
            List<Vector3> followerWay = Corner(followerBody.position, leaderBody.position);
            followerWay.Add(leaderBody.position);
            followerWay.AddRange(ShortOfEnd(leaderBody.position, leaderWay, followGap));
            behind = new Walker(context, followerBody, followerWay);
        }

        while (!leader.Done || (behind != null && !behind.Done))
        {
            float step = speed * GameClock.DeltaTime;
            leader.Advance(step);

            // The gap counts along the way, not in a straight line: round a corner the leader is
            // nearer as the crow flies, and the follower would stop and go every other frame,
            // walking on the spot. Its way ends the gap short of the leader's, so it has room for
            // as much as it has more way left. Waits while it has none, e.g. setting off too close.
            if (behind != null)
                behind.Advance(Mathf.Clamp(behind.Left - leader.Left, 0f, step));

            yield return null;
        }

        leader.Stop(context);
        behind?.Stop(context);
    }

    // The leader's way: a corner to the first point, then point after point.
    private List<Vector3> WayFrom(Vector3 start)
    {
        List<Vector3> way = new();
        foreach (Transform point in points)
        {
            if (point == null)
                continue;

            if (way.Count == 0)
                way.AddRange(Corner(start, point.position));

            way.Add(point.position);
        }

        return way;
    }

    private List<Vector3> Corner(Vector3 from, Vector3 to)
    {
        Vector3 corner = upDownFirst ? new Vector3(from.x, to.y, from.z) : new Vector3(to.x, from.y, from.z);
        return new List<Vector3> { corner };
    }

    // The way cut short by a gap at its end: where the follower stops behind the leader.
    private static List<Vector3> ShortOfEnd(Vector3 start, List<Vector3> way, float gap)
    {
        List<Vector3> points = new() { start };
        points.AddRange(way);
        float left = gap;
        for (int i = points.Count - 1; i > 0; i--)
        {
            float leg = Vector3.Distance(points[i - 1], points[i]);
            if (leg >= left)
            {
                points[i] = Vector3.MoveTowards(points[i], points[i - 1], left);
                points.RemoveAt(0);
                return points.GetRange(0, i);
            }

            left -= leg;
            points.RemoveAt(i);
        }

        return new List<Vector3>();
    }

    public override string ToString()
    {
        return withFollower ? $"{actor} leads {follower} along a path" : $"{actor} walks a path";
    }
}
