using System.Collections.Generic;
using UnityEngine;

// Makes an NPC walk after the player once a story flag is set, e.g. Hideshi travelling with
// Haru through Arc 1. With Stays Put on he stands where he is like any other NPC. The companion
// moves with physics, so walls and furniture stop him. With nothing in the way he walks straight to
// the player; otherwise the way the player walked (the player's trail, which he surely fits), and
// with no trail to take a way around on a small grid lined up with the tiles. Left far behind
// (doorways, story teleports), or stuck for good where the player cannot see him, he jumps after
// the player: to a spot of the trail off screen if there is one, so he walks in rather than
// appears. He never bumps into the player. While following, he also fights at the player's side
// (battleData).
// While the Stays Behind flag is set he waits where he stood, and Haru fights alone. Standing by
// Haru he looks at him; waiting on his own, he turns ahead after a cutscene or a talk like anyone
// (StandingFacesAhead).
// Several companions keep the same distance and never stand inside each other: one
// standing is in the others' way like furniture, so they stop beside him, and with no room by the
// player (a narrow passage) one waits behind another instead of pushing into him. They never hide
// the player from each other.
[RequireComponent(typeof(NpcCharacter))]
public sealed class CompanionFollower : MonoBehaviour
{
    private const float WaypointReachedDistance = 0.08f;
    private const float FacingSwitchRatio = 1.3f;
    private const float WalkAnimationHoldTime = 0.15f;
    private const float StuckRepathTime = 0.5f;
    private const float StuckTeleportTime = 3f;
    // In sight of the player he jumps only after this long: he keeps trying the trail meanwhile.
    private const float StuckTeleportTimeOnScreen = 8f;
    // Caught on something: for this long after, the player's trail comes before the shortcuts.
    private const float PreferTrailTime = 2f;
    // The player's trail: a point every so far walked, the last so many of them. A jump longer than
    // Trail Jump (a door, a story teleport) starts it over.
    private const float TrailSpacing = 0.35f;
    private const int TrailLength = 90;
    private const float TrailJump = 2f;
    // How far past the screen's edge (share of the screen) a spot counts as out of sight.
    private const float OffScreenMargin = 0.04f;
    private const int MaxSearchNodes = 4000;
    // Straight stretches are taken only with this much room to spare (times his feet), so he does
    // not catch on corners.
    private const float PathClearance = 1.15f;
    private const float RepathPlayerMoveDistance = 0.5f;
    // Staying behind this close to an NPC (about half a screen), he is there for their talk (IsPresentAt).
    private const float PresentDistance = 12f;
    // Staying behind this close to the player (right beside him), he is in the player's talk
    // (IsWithPlayer); farther off, though in sight, he is not, e.g. waiting by the bushes while Haru
    // talks to Brannoc a few steps away.
    private const float WithPlayerDistance = 4f;
    // Another companion standing this close (feet to feet, about touching) and between him and the
    // player: he waits behind him.
    private const float QueueTouchDistance = 1.25f;
    private const float QueueBehindDot = 0.7f;

    // The companions in the scene, for keeping out of each other's way every physics step.
    private static readonly List<CompanionFollower> Active = new();

    [SerializeField, StoryFlag] private string followAfterStoryFlag = "a1_home_intro";
    [Tooltip("Stays where he is like any other NPC instead of following. Can be switched in Play mode too.")]
    [SerializeField] private bool staysPut;
    [Tooltip("While this story flag is set he stays behind where he stands (and out of battles), e.g. Hideshi helping at the forge while Haru runs an errand alone. A dialogue sets it and clears it again.")]
    [SerializeField] private string staysBehindStoryFlag = "hideshi_stays_behind";
    [Tooltip("Keep it just outside the NPC talk zone, so the Talk prompt is not shown all the time.")]
    [SerializeField, Min(0.1f)] private float followDistance = 2.8f;
    [Tooltip("How much further than Follow Distance the player must get before the companion sets off again. " +
        "Without it he twitches between walking and standing at the edge of Follow Distance.")]
    [SerializeField, Min(0f)] private float resumeMargin = 0.8f;
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField, Min(1f)] private float teleportDistance = 12f;
    [Tooltip("Feet collider while following: no wider than the player's, so he fits through the same doorways.")]
    [SerializeField] private Vector2 followColliderSize = new(1f, 0.5f);

    [Header("Battle")]
    [Tooltip("How he fights at the player's side while following: health, abilities, battle sprite. Empty = stays out of battles.")]
    [SerializeField] private EnemyData battleData;

    [Header("Pathfinding")]
    [SerializeField, Min(0.1f)] private float cellSize = 0.5f;
    [Tooltip("How far around the companion and the player the way around an obstacle is searched.")]
    [SerializeField, Min(1f)] private float searchMargin = 6f;
    [SerializeField, Min(0.05f)] private float repathInterval = 0.4f;

    private static readonly Vector2Int[] Neighbours =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    private readonly List<Vector2> path = new();
    private readonly List<Vector2> trail = new();
    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private readonly RaycastHit2D[] castResults = new RaycastHit2D[8];
    private readonly List<Collider2D> ignoredColliders = new();

    private NpcCharacter npc;
    private Animator animator;
    private Rigidbody2D body;
    private BoxCollider2D bodyCollider;
    private Vector2 idleColliderSize;
    private Transform player;
    private Collider2D playerCollider;
    private ContactFilter2D obstacleFilter;
    private Vector2 lastPlayerPosition;
    private Vector2 playerHeading = Vector2.down;
    private Vector2 facing = Vector2.down;
    private Vector2 moveDirection;
    private Vector2 lastBodyPosition;
    private bool following;
    private bool walking;
    private bool wasGameplay = true;
    private float nextRepathTime;
    private bool repathNow;
    private Vector2 pathGoal;
    private float commandedStep;
    private float stuckTime;
    private float nextStuckRepath = StuckRepathTime;
    private float preferTrailUntil;
    private float walkAnimationUntil;
    private readonly Dictionary<Collider2D, bool> doorPartCache = new();
    private readonly Dictionary<Collider2D, CompanionFollower> companionPartCache = new();

    public bool IsFollowing => following;

    // Whether a companion takes part in a talk at this spot: walking with the player, or staying
    // behind right here (e.g. Hideshi working the bellows while Haru talks to Garr). Not when he
    // waits somewhere else, or after he has left the story.
    public static bool IsPresentAt(Vector3 spot)
    {
        foreach (CompanionFollower follower in FindObjectsByType<CompanionFollower>(FindObjectsSortMode.None))
        {
            if (follower.isActiveAndEnabled
                && (follower.ShouldFollow || Vector2.Distance(follower.transform.position, spot) <= PresentDistance))
                return true;
        }

        return false;
    }

    // Walking with the player, or staying behind right beside him: he takes part in the player's talk,
    // e.g. his lines with the fox family (StoryCondition Companion With Player). Waiting somewhere
    // else, even close enough to be seen, he does not.
    public static bool IsWithPlayer(Vector3 playerPosition)
    {
        foreach (CompanionFollower follower in FindObjectsByType<CompanionFollower>(FindObjectsSortMode.None))
        {
            if (follower.isActiveAndEnabled
                && (follower.ShouldFollow || Vector2.Distance(follower.transform.position, playerPosition) <= WithPlayerDistance))
                return true;
        }

        return false;
    }

    // The companion walking with the player right now, e.g. to step up in front for a talk he leads.
    public static CompanionFollower FindFollowing()
    {
        foreach (CompanionFollower follower in FindObjectsByType<CompanionFollower>(FindObjectsSortMode.None))
        {
            if (follower.isActiveAndEnabled && follower.ShouldFollow)
                return follower;
        }

        return null;
    }

    // The companions following the player right now, as they join a battle at the player's side.
    public static List<EnemyData> CollectBattleAllies()
    {
        var allies = new List<EnemyData>();
        foreach (CompanionFollower follower in FindObjectsByType<CompanionFollower>(FindObjectsSortMode.None))
        {
            if (follower.battleData != null && follower.isActiveAndEnabled && follower.ShouldFollow)
                allies.Add(follower.battleData);
        }

        return allies;
    }

    private bool ShouldFollow => !staysPut
        && StoryFlagManager.HasFlag(followAfterStoryFlag)
        && (string.IsNullOrWhiteSpace(staysBehindStoryFlag) || !StoryFlagManager.HasFlag(staysBehindStoryFlag));

    private void Awake()
    {
        npc = GetComponent<NpcCharacter>();
        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<BoxCollider2D>();
        if (bodyCollider != null)
            idleColliderSize = bodyCollider.size;

        body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        obstacleFilter = new ContactFilter2D { useTriggers = false };
        obstacleFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void Update()
    {
        bool wasFollowing = following;
        SetFollowing(ShouldFollow);
        if (!following || !TryFindPlayer())
            return;

        // Sets off far from the player, e.g. a debug start inside the house while he stands outside,
        // or a loaded save: at their side at once, before the frame is drawn, not a physics step later.
        if (!wasFollowing && Vector2.Distance(FeetCenter, PlayerFeetCenter) > teleportDistance)
            TeleportNearPlayer(walkInFromOffScreen: false);

        if (!GameStateManager.IsGameplay)
        {
            // Once, so a story event walking the companion is not overridden every frame. Only stops
            // the walk: he keeps looking where he looks, e.g. where a story event turned him.
            if (wasGameplay)
                CharacterWalkAnimation.Apply(animator, false, Vector2.zero);
            wasGameplay = false;
            return;
        }

        if (!wasGameplay)
            ResumeAfterPause();

        bool animateWalk = Time.time < walkAnimationUntil;
        if (animateWalk)
            UpdateFacing(moveDirection);
        else
            UpdateFacing((Vector2)player.position - (Vector2)transform.position);

        CharacterWalkAnimation.Apply(animator, animateWalk, facing);
    }

    private void FixedUpdate()
    {
        if (!following || player == null || !GameStateManager.IsGameplay || !body.simulated)
            return;

        // Physics runs before Update in the frame play comes back.
        if (!wasGameplay)
            ResumeAfterPause();

        Vector2 playerPosition = player.position;
        Vector2 playerStep = playerPosition - lastPlayerPosition;
        if (playerStep.sqrMagnitude > 0.0001f)
            playerHeading = playerStep.normalized;
        lastPlayerPosition = playerPosition;

        // Walk animation follows real movement, held briefly so a single blocked step does not flicker it.
        float moved = Vector2.Distance(body.position, lastBodyPosition);
        lastBodyPosition = body.position;
        if (moved > 0.002f)
            walkAnimationUntil = Time.time + WalkAnimationHoldTime;
        TrackStuck(moved);

        Vector2 feet = FeetCenter;
        Vector2 playerFeet = PlayerFeetCenter;
        float distance = Vector2.Distance(feet, playerFeet);
        RecordTrail(playerFeet);

        if (distance > teleportDistance)
        {
            TeleportNearPlayer(walkInFromOffScreen: true);
            return;
        }

        bool canSeePlayer = HasLineOfSight(feet, playerFeet);
        bool queued = IsQueuedBehindCompanion(feet, playerFeet, distance);
        if (walking && ((distance <= followDistance && canSeePlayer) || queued))
            StopWalking();
        else if (!walking && !queued
            && (distance > followDistance + resumeMargin || (distance > followDistance && !canSeePlayer)))
            StartWalking();

        if (!walking)
        {
            body.linearVelocity = Vector2.zero;
            commandedStep = 0f;
            return;
        }

        bool playerMovedOn = (playerFeet - pathGoal).sqrMagnitude > RepathPlayerMoveDistance * RepathPlayerMoveDistance;
        if (repathNow || (Time.time >= nextRepathTime && (path.Count == 0 || playerMovedOn)))
            RebuildPath(feet, playerFeet);

        while (path.Count > 0 && (path[0] - feet).sqrMagnitude < WaypointReachedDistance * WaypointReachedDistance)
            path.RemoveAt(0);

        // At the end of the way, still not by the player: the next way at once.
        if (path.Count == 0)
            RebuildPath(feet, playerFeet);

        Vector2 waypoint = path.Count > 0 ? path[0] : playerFeet;
        Vector2 toWaypoint = waypoint - feet;
        float waypointDistance = toWaypoint.magnitude;
        if (waypointDistance < 0.0001f)
        {
            body.linearVelocity = Vector2.zero;
            commandedStep = 0f;
            return;
        }

        // Catch up a little faster when falling behind; slower in among trees (Thicket), like the player.
        float speed = (distance > followDistance * 2f ? moveSpeed * 1.5f : moveSpeed) * Thicket.SpeedAt(feet);
        moveDirection = toWaypoint / waypointDistance;
        commandedStep = Mathf.Min(speed * Time.fixedDeltaTime, waypointDistance);
        body.linearVelocity = Vector2.zero;

        // Not into the way of someone walking past on their own (Garr to his forge): he waits until
        // they are by. Not stuck meanwhile.
        if (!BackgroundWalk.InSomeonesWay(feet, transform)
            && BackgroundWalk.InSomeonesWay(feet + moveDirection * commandedStep, transform))
        {
            commandedStep = 0f;
            return;
        }

        body.MovePosition(body.position + moveDirection * commandedStep);
    }

    // Another companion following too stands right in front of him, between him and the player:
    // there is no room by the player, so he waits there (and sets off once that one walks on).
    private bool IsQueuedBehindCompanion(Vector2 feet, Vector2 playerFeet, float distance)
    {
        Vector2 toPlayer = (playerFeet - feet).normalized;
        foreach (CompanionFollower other in Active)
        {
            if (other == this || !other.following || other.walking)
                continue;

            Vector2 otherFeet = other.FeetCenter;
            Vector2 toOther = otherFeet - feet;
            if (toOther.magnitude <= QueueTouchDistance
                && Vector2.Distance(otherFeet, playerFeet) < distance
                && Vector2.Dot(toOther.normalized, toPlayer) >= QueueBehindDot)
                return true;
        }

        return false;
    }

    private Vector2 FeetCenter => bodyCollider != null ? (Vector2)bodyCollider.bounds.center : body.position;

    private Vector2 PlayerFeetCenter => playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)player.position;

    private Vector2 FeetSize
    {
        get
        {
            Vector2 size = bodyCollider != null ? bodyCollider.size : followColliderSize;
            Vector3 scale = transform.lossyScale;
            return new Vector2(Mathf.Abs(size.x * scale.x), Mathf.Abs(size.y * scale.y)) * 0.95f;
        }
    }

    // Back in play after a dialogue or a story event, which may have walked and turned him: go on
    // from where he stands and looks now. Otherwise the step he was taking when it began plays on
    // for a moment (Time.time stood still meanwhile), e.g. a turn back towards the road he walked
    // up, after a cutscene left him facing Haru.
    private void ResumeAfterPause()
    {
        wasGameplay = true;
        StopWalking();
        lastBodyPosition = body.position;
        lastPlayerPosition = player.position;
        walkAnimationUntil = 0f;
        if (CharacterWalkAnimation.TryGetFacing(animator, out Vector2 shown))
            facing = shown;
    }

    private void StartWalking()
    {
        walking = true;
        stuckTime = 0f;
        repathNow = true;
    }

    private void StopWalking()
    {
        walking = false;
        path.Clear();
        body.linearVelocity = Vector2.zero;
    }

    // Compares the last physics step with the step asked for: pressed against something, look for
    // another way every half second, the player's trail first; as a last resort jump after the
    // player, at once only where the player cannot see it.
    private void TrackStuck(float moved)
    {
        if (!walking || commandedStep <= 0f || moved >= commandedStep * 0.1f)
        {
            stuckTime = 0f;
            nextStuckRepath = StuckRepathTime;
            return;
        }

        stuckTime += Time.fixedDeltaTime;
        if (stuckTime >= nextStuckRepath)
        {
            nextStuckRepath += StuckRepathTime;
            repathNow = true;
            preferTrailUntil = Time.time + PreferTrailTime;
        }

        bool farBehind = Vector2.Distance(FeetCenter, PlayerFeetCenter) > followDistance * 2f;
        if (farBehind && stuckTime >= StuckTeleportTime && (!IsOnScreen(FeetCenter) || stuckTime >= StuckTeleportTimeOnScreen))
            TeleportNearPlayer(walkInFromOffScreen: true);
    }

    // The player's feet every Trail Spacing walked: a way the companion surely fits through.
    private void RecordTrail(Vector2 playerFeet)
    {
        if (trail.Count > 0)
        {
            float step = Vector2.Distance(trail[^1], playerFeet);
            if (step > TrailJump)
                trail.Clear();
            else if (step < TrailSpacing)
                return;
        }

        trail.Add(playerFeet);
        if (trail.Count > TrailLength)
            trail.RemoveAt(0);
    }

    private static bool IsOnScreen(Vector2 point)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return true;

        Vector3 viewport = camera.WorldToViewportPoint(point);
        return viewport.x >= -OffScreenMargin && viewport.x <= 1f + OffScreenMargin
            && viewport.y >= -OffScreenMargin && viewport.y <= 1f + OffScreenMargin;
    }

    // walkInFromOffScreen: to the newest spot of the player's trail out of sight, so he walks in
    // from there; with none, or for a jump nobody is to see (a loaded save), right behind the player.
    private void TeleportNearPlayer(bool walkInFromOffScreen)
    {
        Vector2 playerFeet = PlayerFeetCenter;
        Vector2 feetOffset = FeetCenter - body.position;
        if (walkInFromOffScreen && TryFindOffScreenTrailSpot(playerFeet, out Vector2 offScreen))
        {
            npc.TeleportTo(offScreen - feetOffset);
            lastBodyPosition = body.position;
            StopWalking();
            UpdateFacing(playerFeet - offScreen);
            return;
        }

        Vector2 target = playerFeet - playerHeading * followDistance;

        // Behind the player first, then around them, closer each round, on a free spot they can see.
        for (float radius = followDistance; radius >= cellSize; radius -= cellSize)
        {
            bool found = false;
            for (int i = 0; i < 8 && !found; i++)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, 45f * i) * -playerHeading;
                Vector2 candidate = playerFeet + direction * radius;
                if (IsWalkable(candidate, FeetSize) && HasLineOfSight(candidate, playerFeet))
                {
                    target = candidate;
                    found = true;
                }
            }

            if (found)
                break;
        }

        npc.TeleportTo(target - feetOffset);
        lastBodyPosition = body.position;
        StopWalking();
        UpdateFacing(playerHeading);
    }

    private bool TryFindOffScreenTrailSpot(Vector2 playerFeet, out Vector2 spot)
    {
        for (int i = trail.Count - 1; i >= 0; i--)
        {
            Vector2 point = trail[i];
            if (Vector2.Distance(point, playerFeet) > teleportDistance)
                break;

            if (!IsOnScreen(point) && IsWalkable(point, FeetSize))
            {
                spot = point;
                return true;
            }
        }

        spot = default;
        return false;
    }

    // Straight there if nothing is in the way. Otherwise the way the player walked (the trail): he
    // surely fits where the player went. With no trail to take, the shortest way on the grid; with
    // none either, along the trail from its point nearest to him, sliding along whatever is in the
    // way, never pushing straight at it. Just caught on something: no straight line, that is what he
    // got caught on.
    private void RebuildPath(Vector2 from, Vector2 goal)
    {
        nextRepathTime = Time.time + repathInterval;
        repathNow = false;
        pathGoal = goal;
        path.Clear();

        if (Time.time >= preferTrailUntil && CanWalkClear(from, goal))
        {
            path.Add(goal);
            return;
        }

        if (TryTrailPath(from))
            return;

        if (TryFindPath(from, goal))
        {
            SmoothPath(from);
            return;
        }

        if (TryNearestTrailPath(from))
            return;

        path.Add(goal);
    }

    // From the newest point of the player's trail he can walk to in a straight line, then along it.
    private bool TryTrailPath(Vector2 from)
    {
        for (int i = trail.Count - 1; i >= 0; i--)
        {
            if (!CanWalkClear(from, trail[i]))
                continue;

            for (int j = i; j < trail.Count; j++)
                path.Add(trail[j]);

            SmoothPath(from);
            return path.Count > 0;
        }

        return false;
    }

    private bool TryNearestTrailPath(Vector2 from)
    {
        int nearest = -1;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < trail.Count; i++)
        {
            float d = (trail[i] - from).sqrMagnitude;
            if (d < nearestDistance)
            {
                nearestDistance = d;
                nearest = i;
            }
        }

        if (nearest < 0)
            return false;

        for (int j = nearest; j < trail.Count; j++)
            path.Add(trail[j]);

        return true;
    }

    // A* on a grid around the companion and the player. Any free cell within Follow Distance of
    // the player, with a clear view of them, is a good place to stop.
    private bool TryFindPath(Vector2 from, Vector2 goal)
    {
        // The grid lines up with the world (and the tiles), not with where he stands: a doorway
        // between walls on tile edges then has cells down its middle.
        Vector2 origin = new(Mathf.Round(from.x / cellSize) * cellSize, Mathf.Round(from.y / cellSize) * cellSize);
        Vector2 min = Vector2.Min(from, goal) - Vector2.one * searchMargin;
        Vector2 max = Vector2.Max(from, goal) + Vector2.one * searchMargin;
        Vector2Int minCell = ToCell(min, origin);
        Vector2Int maxCell = ToCell(max, origin);

        var walkable = new Dictionary<Vector2Int, bool>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var cost = new Dictionary<Vector2Int, float> { [Vector2Int.zero] = 0f };
        var open = new CellQueue();
        open.Push(Vector2Int.zero, Heuristic(origin, goal));
        var closed = new HashSet<Vector2Int>();

        while (open.Count > 0 && closed.Count < MaxSearchNodes)
        {
            Vector2Int cell = open.Pop();
            if (!closed.Add(cell))
                continue;

            Vector2 position = ToWorld(cell, origin);
            if (cell != Vector2Int.zero && Vector2.Distance(position, goal) <= followDistance && HasLineOfSight(position, goal))
            {
                BuildPath(cell, cameFrom, origin);
                return true;
            }

            foreach (Vector2Int offset in Neighbours)
            {
                Vector2Int next = cell + offset;
                if (closed.Contains(next) || next.x < minCell.x || next.x > maxCell.x || next.y < minCell.y || next.y > maxCell.y)
                    continue;

                if (!IsWalkableCell(next, origin, walkable))
                    continue;

                // No cutting corners past a wall.
                bool diagonal = offset.x != 0 && offset.y != 0;
                if (diagonal && (!IsWalkableCell(new Vector2Int(next.x, cell.y), origin, walkable)
                    || !IsWalkableCell(new Vector2Int(cell.x, next.y), origin, walkable)))
                    continue;

                float nextCost = cost[cell] + (diagonal ? 1.4142f : 1f) * cellSize;
                if (cost.TryGetValue(next, out float knownCost) && knownCost <= nextCost)
                    continue;

                cost[next] = nextCost;
                cameFrom[next] = cell;
                open.Push(next, nextCost + Heuristic(ToWorld(next, origin), goal));
            }
        }

        return false;
    }

    private float Heuristic(Vector2 position, Vector2 goal)
    {
        return Mathf.Max(0f, Vector2.Distance(position, goal) - followDistance);
    }

    private void BuildPath(Vector2Int cell, Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2 origin)
    {
        while (cell != Vector2Int.zero)
        {
            path.Insert(0, ToWorld(cell, origin));
            cell = cameFrom[cell];
        }
    }

    // Skip waypoints that can be reached in a straight line with room to spare, so he does not
    // zigzag along the grid or the trail, nor catch on a corner he cut.
    private void SmoothPath(Vector2 from)
    {
        var smoothed = new List<Vector2>();
        Vector2 current = from;
        int index = 0;
        while (index < path.Count)
        {
            int furthest = index;
            for (int i = path.Count - 1; i > index; i--)
            {
                if (CanWalkClear(current, path[i]))
                {
                    furthest = i;
                    break;
                }
            }

            current = path[furthest];
            smoothed.Add(current);
            index = furthest + 1;
        }

        path.Clear();
        path.AddRange(smoothed);
    }

    private Vector2Int ToCell(Vector2 position, Vector2 origin)
    {
        Vector2 local = (position - origin) / cellSize;
        return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));
    }

    private Vector2 ToWorld(Vector2Int cell, Vector2 origin)
    {
        return origin + (Vector2)cell * cellSize;
    }

    private bool IsWalkableCell(Vector2Int cell, Vector2 origin, Dictionary<Vector2Int, bool> cache)
    {
        if (!cache.TryGetValue(cell, out bool free))
        {
            free = IsWalkable(ToWorld(cell, origin), Vector2.Max(FeetSize, Vector2.one * cellSize * 1.05f));
            cache[cell] = free;
        }

        return free;
    }

    // Grid probes are at least one cell big: walls are thin outlines, and with smaller probes a wall
    // could sit in the gap between two neighbouring cells.
    private bool IsWalkable(Vector2 feetCenter, Vector2 size)
    {
        // Where someone walking past on their own is about to be: the way round goes behind them.
        if (BackgroundWalk.InSomeonesWay(feetCenter, transform))
            return false;

        int count = Physics2D.OverlapBox(feetCenter, size, 0f, obstacleFilter, overlapResults);
        for (int i = 0; i < count; i++)
        {
            if (!IsIgnored(overlapResults[i]))
                return false;
        }

        return true;
    }

    // A straight stretch with room to spare (Path Clearance). The wider box already touches what he
    // stands against, e.g. a haystack he was walked into: then only that is in its way, and his own
    // size tells whether the stretch is free (it leads away or along it) or not.
    private bool CanWalkClear(Vector2 from, Vector2 to)
    {
        if (CrossesSomeonesWay(from, to))
            return false;

        Way way = Cast(from, to, FeetSize * PathClearance);
        return way == Way.Clear || (way == Way.TouchingAtStart && Cast(from, to, FeetSize) == Way.Clear);
    }

    // Into the way of someone walking past on their own, from outside it (already in it, any way out
    // will do).
    private bool CrossesSomeonesWay(Vector2 from, Vector2 to)
    {
        if (BackgroundWalk.InSomeonesWay(from, transform))
            return false;

        float length = Vector2.Distance(from, to);
        int samples = Mathf.CeilToInt(length / 0.3f);
        for (int i = 1; i <= samples; i++)
        {
            if (BackgroundWalk.InSomeonesWay(Vector2.Lerp(from, to, i / (float)samples), transform))
                return true;
        }

        return false;
    }

    private enum Way
    {
        Clear,
        Blocked,
        // Only something the box already overlaps where it starts.
        TouchingAtStart
    }

    private Way Cast(Vector2 from, Vector2 to, Vector2 size)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.0001f)
            return Way.Clear;

        bool touching = false;
        int count = Physics2D.BoxCast(from, size, 0f, delta / distance, obstacleFilter, castResults, distance);
        for (int i = 0; i < count; i++)
        {
            if (IsIgnored(castResults[i].collider))
                continue;

            if (castResults[i].distance <= 0.0001f)
            {
                touching = true;
                continue;
            }

            return Way.Blocked;
        }

        return touching ? Way.TouchingAtStart : Way.Clear;
    }

    private bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        int count = Physics2D.Linecast(from, to, obstacleFilter, castResults);
        for (int i = 0; i < count; i++)
        {
            if (!IsIgnored(castResults[i].collider, true))
                return false;
        }

        return true;
    }

    // Himself, the player, and doors that open on their own when he walks up are not obstacles.
    // Another companion following too never hides the player, and is in the way only standing:
    // walking, he moves on.
    private bool IsIgnored(Collider2D other, bool forSight = false)
    {
        if (other == null || ignoredColliders.Contains(other))
            return true;

        // Someone walking past on their own passes through him: their way is kept clear instead
        // (BackgroundWalk.InSomeonesWay).
        if (BackgroundWalk.IsWalkingPast(other) && other.attachedRigidbody != body)
            return true;

        if (!companionPartCache.TryGetValue(other, out CompanionFollower companion))
        {
            companion = other.GetComponentInParent<CompanionFollower>();
            companionPartCache[other] = companion;
        }

        if (companion != null && companion != this && companion.following)
            return forSight || companion.walking;

        if (!doorPartCache.TryGetValue(other, out bool automaticDoor))
        {
            Door door = other.GetComponentInParent<Door>();
            automaticDoor = door != null && door.OpensAutomatically;
            doorPartCache[other] = automaticDoor;
        }

        return automaticDoor;
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        // Switch between facing up/down and left/right only once the other axis clearly wins,
        // so walking or looking diagonally does not flip the animation back and forth.
        float x = Mathf.Abs(direction.x);
        float y = Mathf.Abs(direction.y);
        bool vertical = facing.x == 0f;
        if (vertical && x > y * FacingSwitchRatio)
            vertical = false;
        else if (!vertical && y > x * FacingSwitchRatio)
            vertical = true;

        facing = vertical
            ? (direction.y > 0f ? Vector2.up : Vector2.down)
            : (direction.x > 0f ? Vector2.right : Vector2.left);
    }

    private bool TryFindPlayer()
    {
        if (player != null)
            return true;

        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
        if (movement == null)
            return false;

        player = movement.transform;
        playerCollider = movement.GetComponent<Collider2D>();
        lastPlayerPosition = player.position;
        IgnorePlayerCollisions(following);
        return true;
    }

    private void SetFollowing(bool value)
    {
        if (following == value)
            return;

        following = value;
        StopWalking();

        // Following: walls stop him, the player walks through him. Standing: nobody pushes him around.
        body.bodyType = value ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        body.linearVelocity = Vector2.zero;
        lastBodyPosition = body.position;
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
            bodyCollider.size = value ? Vector2.Min(idleColliderSize, followColliderSize) : idleColliderSize;
        }

        IgnorePlayerCollisions(value);
    }

    private void IgnorePlayerCollisions(bool ignore)
    {
        ignoredColliders.Clear();
        ignoredColliders.AddRange(GetComponentsInChildren<Collider2D>(true));
        if (player == null)
            return;

        foreach (Collider2D playerPart in player.GetComponentsInChildren<Collider2D>(true))
        {
            ignoredColliders.Add(playerPart);
            if (bodyCollider != null)
                Physics2D.IgnoreCollision(bodyCollider, playerPart, ignore);
        }
    }

    // Binary min-heap of grid cells by score, for the A* open set.
    private sealed class CellQueue
    {
        private readonly List<(Vector2Int cell, float score)> items = new();

        public int Count => items.Count;

        public void Push(Vector2Int cell, float score)
        {
            items.Add((cell, score));
            int child = items.Count - 1;
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (items[parent].score <= items[child].score)
                    break;

                (items[parent], items[child]) = (items[child], items[parent]);
                child = parent;
            }
        }

        public Vector2Int Pop()
        {
            Vector2Int top = items[0].cell;
            int last = items.Count - 1;
            items[0] = items[last];
            items.RemoveAt(last);

            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                if (left >= items.Count)
                    break;

                int right = left + 1;
                int smallest = right < items.Count && items[right].score < items[left].score ? right : left;
                if (items[parent].score <= items[smallest].score)
                    break;

                (items[parent], items[smallest]) = (items[smallest], items[parent]);
                parent = smallest;
            }

            return top;
        }
    }
}
