using System.Collections.Generic;
using UnityEngine;

// Walks a character somewhere while the player plays on (Walk Off step), e.g. Hideshi going over to
// wait by Eldon's tower after the ground lesson while Haru already sets off to gather. Added to the
// character for the walk and removed at its end. It sets off once the story event that sent it is
// over, goes around walls, props and people in the way (StoryPath), stands still while a dialogue or
// a menu is open, and at the end turns the way it should look; the place is saved, as after a story
// walk. On the way it passes through the player instead of pushing him aside; it blocks him again
// once it stands and he is out of its way. Another story event taking the character over, or a
// companion setting off after the player again, ends the walk where it is.
[DisallowMultipleComponent]
public sealed class BackgroundWalk : MonoBehaviour
{
    private readonly List<Vector2> way = new();
    private readonly List<(Collider2D mine, Collider2D players)> passingThrough = new();
    private Vector2 destination;
    private float speed;
    private Vector2 facingAtEnd;
    private bool avoidObstacles;
    private bool started;
    private bool arrived;
    private int corner;
    private Vector2 heading = Vector2.down;
    private Animator animator;
    private Rigidbody2D body;
    private CompanionFollower follower;

    // facingAtEnd: zero = the way it walked.
    public static void Begin(Transform walker, Vector2 destination, float speed, Vector2 facingAtEnd, bool avoidObstacles)
    {
        if (walker == null)
            return;

        if (!walker.TryGetComponent(out BackgroundWalk walk))
            walk = walker.gameObject.AddComponent<BackgroundWalk>();

        walk.destination = destination;
        walk.speed = speed;
        walk.facingAtEnd = facingAtEnd;
        walk.avoidObstacles = avoidObstacles;
        walk.started = false;
        walk.arrived = false;
        walk.enabled = true;
    }

    // Where a walk would have ended, at once and without walking (a skipped Walk Off step): standing
    // there the way it should look, the place saved as after the walk.
    public static void Place(Transform walker, Vector2 destination, Vector2 facingAtEnd)
    {
        if (walker == null)
            return;

        if (walker.TryGetComponent(out BackgroundWalk walk))
            Destroy(walk);

        Vector3 position = new(destination.x, destination.y, walker.position.z);
        if (walker.TryGetComponent(out Rigidbody2D body))
            body.position = destination;
        walker.position = position;

        Vector2 look = facingAtEnd != Vector2.zero ? facingAtEnd : Vector2.down;
        FlipSpriteToFace.Apply(walker, look);
        CharacterWalkAnimation.Apply(StoryEventContext.GetAnimator(walker), false, look);
        if (walker.TryGetComponent(out NpcCharacter npc))
            ExplorationPlayerSession.SaveNpcPosition(npc.PersistenceKey, position);
    }

    private void Awake()
    {
        animator = StoryEventContext.GetAnimator(transform);
        body = GetComponent<Rigidbody2D>();
        follower = GetComponent<CompanionFollower>();
    }

    private void OnDestroy()
    {
        StopPassingThroughPlayer();
    }

    private Vector2 Position => body != null && body.simulated ? body.position : (Vector2)transform.position;

    // Whether something else moves it right now. A story event still playing when the walk was sent
    // (the one that sent it) is waited out; one taking it over on the way ends the walk.
    private bool Busy => StoryEventContext.IsControlledByStory(transform) || follower != null && follower.IsFollowing;

    private void Update()
    {
        if (Busy)
        {
            if (started)
                Destroy(this);
            return;
        }

        if (!started || arrived)
            return;

        // Every frame: the walk animation must not stop between two physics steps. Stands while a
        // dialogue or a menu has the game.
        bool walking = GameStateManager.IsGameplay;
        if (walking)
            FlipSpriteToFace.Apply(transform, heading);
        CharacterWalkAnimation.Apply(animator, walking, heading);
    }

    private void FixedUpdate()
    {
        if (Busy || !GameStateManager.IsGameplay)
            return;

        if (arrived)
        {
            if (!PlayerInTheWay())
                Destroy(this);
            return;
        }

        if (!started)
            SetOff();

        Vector2 position = Position;
        while (corner < way.Count && (way[corner] - position).sqrMagnitude <= 0.0001f)
            corner++;

        if (corner >= way.Count)
        {
            Arrive(position);
            return;
        }

        heading = way[corner] - position;
        // Slower in among trees (Thicket), like the player.
        float step = speed * Thicket.SpeedAt(Thicket.FeetOf(this)) * Time.fixedDeltaTime;
        Vector2 next = Vector2.MoveTowards(position, way[corner], step);
        if (body != null && body.simulated)
            body.MovePosition(next);
        else
            transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    private void SetOff()
    {
        started = true;
        corner = 0;
        way.Clear();
        List<Vector2> around = avoidObstacles ? FindWayAround() : null;
        if (around != null)
            way.AddRange(around);
        else
            way.Add(destination);

        PassThroughPlayer();
    }

    // Its own feet are not in its way: its physics is off while the way is searched.
    private List<Vector2> FindWayAround()
    {
        bool simulated = body != null && body.simulated;
        if (simulated)
            body.simulated = false;

        List<Vector2> around = StoryPath.Find(transform.position, destination);

        if (simulated)
            body.simulated = true;

        return around;
    }

    // Solid colliders only: the talk zones still notice the player.
    private void PassThroughPlayer()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
            return;

        Collider2D[] players = player.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D mine in GetComponentsInChildren<Collider2D>())
        {
            if (mine.isTrigger)
                continue;

            foreach (Collider2D theirs in players)
            {
                if (theirs.isTrigger)
                    continue;

                Physics2D.IgnoreCollision(mine, theirs, true);
                passingThrough.Add((mine, theirs));
            }
        }
    }

    // A companion following again goes on walking through the player (CompanionFollower).
    private void StopPassingThroughPlayer()
    {
        if (follower == null || !follower.IsFollowing)
        {
            foreach ((Collider2D mine, Collider2D theirs) in passingThrough)
            {
                if (mine != null && theirs != null)
                    Physics2D.IgnoreCollision(mine, theirs, false);
            }
        }

        passingThrough.Clear();
    }

    private bool PlayerInTheWay()
    {
        foreach ((Collider2D mine, Collider2D theirs) in passingThrough)
        {
            if (mine != null && theirs != null && mine.bounds.Intersects(theirs.bounds))
                return true;
        }

        return false;
    }

    // Stays on until the player is out of its way, so he is not shoved off when it blocks him again.
    private void Arrive(Vector2 position)
    {
        arrived = true;
        Vector2 look = facingAtEnd != Vector2.zero ? facingAtEnd : heading;
        FlipSpriteToFace.Apply(transform, look);
        CharacterWalkAnimation.Apply(animator, false, look);
        if (TryGetComponent(out NpcCharacter npc))
            ExplorationPlayerSession.SaveNpcPosition(npc.PersistenceKey, new Vector3(position.x, position.y, transform.position.z));
    }
}
