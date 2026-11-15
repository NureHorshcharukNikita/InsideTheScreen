using System.Collections.Generic;
using UnityEngine;

// Walks a character somewhere while the player plays on (Walk Off step), e.g. Hideshi going over to
// wait by Eldon's tower after the ground lesson while Haru already sets off to gather. Added to the
// character for the walk and removed at its end. It sets off once the story event that sent it is
// over, goes around walls, props and people in the way (StoryPath), stands still while a dialogue or
// a menu is open, and at the end turns the way it should look; the place is saved, as after a story
// walk. On the way it passes through the player and his companion instead of pushing them aside (or
// carrying them along); it blocks them again once it stands and they are out of its way. Another story event taking the character over, or a
// companion setting off after the player again, ends the walk where it is.
// It can come out of somewhere as it sets off (fading in, a door heard if it is on screen), e.g. Garr
// out of his barn, and call a story event when the player gets in its way, e.g. Garr telling Haru off:
// it stands while that plays and walks on after. Once that event is done it just walks through him.
// Walks During Talks: it goes on walking while a dialogue with someone is open (time stands still then,
// so it walks on the game clock), not in a cutscene, nor when it is the one talking; walking into the
// player meanwhile it waits in front of him for the end of the talk, and the event it calls plays then.
// Points on the way (Via): it walks through them in order, e.g. along the roads instead of across
// the grass right behind a house, each stretch around what is in the way.
[DisallowMultipleComponent]
public sealed class BackgroundWalk : MonoBehaviour
{
    private const float ComeOutDuration = 0.35f;
    // The player in its way: in front of it, this far ahead of its feet at most and this far to
    // either side of where it is heading.
    private const float InTheWayAhead = 1.3f;
    private const float InTheWaySide = 0.6f;
    // The stretch it needs on its way, which a companion keeps out of (InSomeonesWay): from a little
    // behind its feet to this far ahead, this far to either side.
    private const float WayBehind = 0.8f;
    private const float WayAhead = 2.6f;
    private const float WaySide = 1.1f;

    // The walks under way right now.
    private static readonly List<BackgroundWalk> Walks = new();

    private readonly List<Vector2> way = new();
    // The points on the way, then the destination; and for each corner of the way, which of them it
    // leads to.
    private readonly List<Vector2> stops = new();
    private readonly List<int> wayStops = new();
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
    private Transform player;
    private bool comesOut;
    private AudioClip comeOutSound;
    private float comeOutVolume;
    // Seconds into fading in as it comes out; below zero = not fading.
    private float comeOutTime = -1f;
    private StoryEvent whenInTheWay;
    // The event it called and stands still for until it is over.
    private StoryEvent standingFor;
    private bool walksDuringTalks;
    // In a talk, standing in front of the player until it is over (then its event plays).
    private bool waitingInTheWay;
    private NpcCharacter npc;
    private bool animatorOnGameClock;
    private AnimatorUpdateMode animatorMode;

    // facingAtEnd: zero = the way it walked.
    public static BackgroundWalk Begin(Transform walker, Vector2 destination, float speed, Vector2 facingAtEnd, bool avoidObstacles,
        bool walksDuringTalks = false, IReadOnlyList<Vector2> via = null)
    {
        if (walker == null)
            return null;

        if (!walker.TryGetComponent(out BackgroundWalk walk))
            walk = walker.gameObject.AddComponent<BackgroundWalk>();

        walk.destination = destination;
        walk.stops.Clear();
        if (via != null)
            walk.stops.AddRange(via);
        walk.stops.Add(destination);
        walk.way.Clear();
        walk.wayStops.Clear();
        walk.corner = 0;
        walk.speed = speed;
        walk.facingAtEnd = facingAtEnd;
        walk.avoidObstacles = avoidObstacles;
        walk.started = false;
        walk.arrived = false;
        walk.comesOut = false;
        walk.comeOutSound = null;
        walk.comeOutTime = -1f;
        walk.whenInTheWay = null;
        walk.standingFor = null;
        walk.walksDuringTalks = walksDuringTalks;
        walk.waitingInTheWay = false;
        walk.enabled = true;
        return walk;
    }

    // It comes out of somewhere as it sets off: out of sight until then (also while its object is
    // still turned off, e.g. Garr in his barn), then fading in where it stands, the sound heard if it
    // is on screen.
    public void ComeOut(AudioClip sound, float volume)
    {
        comesOut = true;
        comeOutSound = sound;
        comeOutVolume = volume;
        SetAlpha(0f);
    }

    // Played when the player gets in its way while it walks, if it can start then.
    public void CallWhenInTheWay(StoryEvent storyEvent)
    {
        whenInTheWay = storyEvent;
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlayMode()
    {
        Walks.Clear();
    }

    // Set off and not there yet.
    public bool IsOnItsWay => started && !arrived && isActiveAndEnabled;

    // In the way of someone walking past on their own: where they stand or just in front of them, the
    // way they head. E.g. Hideshi waits for Garr to go by, or goes round behind him.
    public static bool InSomeonesWay(Vector2 point, Transform except)
    {
        foreach (BackgroundWalk walk in Walks)
        {
            if (walk != null && walk.transform != except && walk.IsOnItsWay && walk.Covers(point))
                return true;
        }

        return false;
    }

    // A collider of someone on their way: they pass through the player and his companion, so it is
    // no obstacle to walk round.
    public static bool IsWalkingPast(Collider2D part)
    {
        if (part == null)
            return false;

        Rigidbody2D owner = part.attachedRigidbody;
        BackgroundWalk walk = owner != null ? owner.GetComponent<BackgroundWalk>() : part.GetComponentInParent<BackgroundWalk>();
        return walk != null && walk.IsOnItsWay;
    }

    private bool Covers(Vector2 point)
    {
        Vector2 direction = heading.sqrMagnitude > 0.0001f ? heading.normalized : Vector2.down;
        Vector2 offset = point - Position;
        float along = Vector2.Dot(offset, direction);
        float side = Mathf.Abs(offset.x * direction.y - offset.y * direction.x);
        return along > -WayBehind && along < WayAhead && side < WaySide;
    }

    private void OnEnable()
    {
        if (!Walks.Contains(this))
            Walks.Add(this);
    }

    private void OnDisable()
    {
        Walks.Remove(this);
    }

    private void Awake()
    {
        animator = StoryEventContext.GetAnimator(transform);
        body = GetComponent<Rigidbody2D>();
        follower = GetComponent<CompanionFollower>();
        npc = GetComponent<NpcCharacter>();
    }

    private void OnDestroy()
    {
        StopPassingThroughPlayer();

        // An event taking it over keeps its animator on the game clock itself and puts it back after.
        if (!StoryEventContext.IsControlledByStory(transform))
            AnimatorBackOnGameTime();

        // Ended halfway into coming out: seen in full anyway.
        if (comeOutTime >= 0f)
            SetAlpha(1f);
    }

    private Vector2 Position => body != null && body.simulated ? body.position : (Vector2)transform.position;

    // Whether something else moves it right now. A story event still playing when the walk was sent
    // (the one that sent it) is waited out; one taking it over on the way ends the walk.
    private bool Busy => StoryEventContext.IsControlledByStory(transform) || follower != null && follower.IsFollowing;

    // A dialogue or another event plays and it walks on through it (Walks During Talks).
    private bool WalkingThroughTalk => walksDuringTalks && GameStateManager.IsDialogue && !StoryEvent.CutscenePlaying && !IsTalking;

    // The one talking or trading: it stands for that.
    private bool IsTalking => npc != null && (npc.State == NpcState.Talking || npc.State == NpcState.Trading);

    private void Update()
    {
        if (comeOutTime >= 0f)
        {
            comeOutTime += WalkingThroughTalk ? GameClock.DeltaTime : Time.deltaTime;
            SetAlpha(Mathf.Clamp01(comeOutTime / ComeOutDuration));
            if (comeOutTime >= ComeOutDuration)
                comeOutTime = -1f;
        }

        if (WaitingForEvent())
            return;

        if (Busy)
        {
            if (started)
                Destroy(this);
            return;
        }

        // Time stands still in a talk: no physics steps, so it walks on here, on the game clock.
        bool throughTalk = WalkingThroughTalk;
        if (throughTalk)
            Walk(GameClock.DeltaTime, false);

        if (!started || arrived)
            return;

        // Every frame: the walk animation must not stop between two physics steps. Stands while a
        // dialogue or a menu has the game, unless it walks through talks.
        bool walking = (GameStateManager.IsGameplay || throughTalk) && !waitingInTheWay;
        if (walking)
            FlipSpriteToFace.Apply(transform, heading);
        CharacterWalkAnimation.Apply(animator, walking, heading);
    }

    private void FixedUpdate()
    {
        if (WaitingForEvent() || Busy || !GameStateManager.IsGameplay)
            return;

        Walk(Time.fixedDeltaTime, true);
    }

    // One step of the walk: by physics in play, moved straight on in a talk.
    private void Walk(float deltaTime, bool byPhysics)
    {
        waitingInTheWay = false;
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

        // The player in its way: its event, or in a talk standing in front of him until it is over.
        if (whenInTheWay != null && PlayerRightAhead(position, heading))
        {
            bool inPlay = GameStateManager.IsGameplay;
            if (inPlay ? whenInTheWay.TryPlay() : whenInTheWay.IsReady)
            {
                if (inPlay)
                    standingFor = whenInTheWay;
                else
                    waitingInTheWay = true;
                CharacterWalkAnimation.Apply(animator, false, heading);
                return;
            }
        }

        // Slower in among trees (Thicket), like the player.
        float step = speed * Thicket.SpeedAt(Thicket.FeetOf(this)) * deltaTime;
        Vector2 next = Vector2.MoveTowards(position, way[corner], step);
        if (body != null && body.simulated)
        {
            if (byPhysics)
                body.MovePosition(next);
            else
                body.position = next;
        }

        if (!byPhysics || body == null || !body.simulated)
            transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    // Walking through talks, its animation runs while time stands still.
    private void AnimatorOnGameClock()
    {
        if (animator == null || animatorOnGameClock)
            return;

        animatorMode = animator.updateMode;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animatorOnGameClock = true;
    }

    private void AnimatorBackOnGameTime()
    {
        if (animator == null || !animatorOnGameClock)
            return;

        animator.updateMode = animatorMode;
        animatorOnGameClock = false;
    }

    private void SetOff()
    {
        started = true;
        FindWay();
        PassThroughPlayer();
        if (walksDuringTalks)
            AnimatorOnGameClock();

        if (comesOut)
        {
            comesOut = false;
            comeOutTime = 0f;
            SetAlpha(0f);
            if (comeOutSound != null && IsOnScreen())
                AudioManager.TryPlaySfx(comeOutSound, comeOutVolume);
        }
    }

    // From where it stands now through the points still ahead: also again after an event it called,
    // which may have moved people about.
    private void FindWay()
    {
        int firstStop = corner < wayStops.Count ? wayStops[corner] : 0;
        corner = 0;
        way.Clear();
        wayStops.Clear();

        // Its own feet are not in its way: its physics is off while the way is searched.
        bool simulated = body != null && body.simulated;
        if (simulated)
            body.simulated = false;

        Vector2 from = transform.position;
        for (int stop = firstStop; stop < stops.Count; stop++)
        {
            List<Vector2> around = avoidObstacles ? StoryPath.Find(from, stops[stop]) : null;
            if (around != null)
                way.AddRange(around);
            else
                way.Add(stops[stop]);

            while (wayStops.Count < way.Count)
                wayStops.Add(stop);
            from = stops[stop];
        }

        if (simulated)
            body.simulated = true;
    }

    // While the event it called plays, it stands still and nothing else counts (that event also takes
    // it over); once it is over, on its way again from where it is.
    private bool WaitingForEvent()
    {
        if (standingFor == null)
            return false;

        if (standingFor.IsPlaying)
            return true;

        standingFor = null;
        if (started && !arrived)
            FindWay();
        return false;
    }

    private bool PlayerRightAhead(Vector2 position, Vector2 direction)
    {
        if (player == null || direction.sqrMagnitude < 0.0001f)
            return false;

        direction.Normalize();
        Vector2 offset = (Vector2)player.position - position;
        float ahead = Vector2.Dot(offset, direction);
        float side = Mathf.Abs(offset.x * direction.y - offset.y * direction.x);
        return ahead > 0f && ahead <= InTheWayAhead && side <= InTheWaySide;
    }

    private bool IsOnScreen()
    {
        Camera view = Camera.main;
        if (view == null)
            return false;

        Vector3 point = view.WorldToViewportPoint(transform.position);
        return point.z > 0f && point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;
    }

    // Its own sprite, found also while its object is turned off.
    private void SetAlpha(float alpha)
    {
        SpriteRenderer sprite = TryGetComponent(out SpriteRenderer own) ? own : GetComponentInChildren<SpriteRenderer>(true);
        if (sprite == null)
            return;

        Color color = sprite.color;
        color.a = alpha;
        sprite.color = color;
    }

    // Through the player and whoever walks with him (Hideshi), so it neither shoves them aside nor
    // carries them along, e.g. Garr walking on after telling Haru off with his father beside him.
    // Solid colliders only: the talk zones still notice the player.
    private void PassThroughPlayer()
    {
        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
        if (movement == null)
            return;

        player = movement.transform;
        PassThrough(movement.GetComponentsInChildren<Collider2D>());
        foreach (CompanionFollower companion in FindObjectsByType<CompanionFollower>(FindObjectsSortMode.None))
        {
            if (companion != follower)
                PassThrough(companion.GetComponentsInChildren<Collider2D>());
        }
    }

    private void PassThrough(Collider2D[] others)
    {
        foreach (Collider2D mine in GetComponentsInChildren<Collider2D>())
        {
            if (mine.isTrigger)
                continue;

            foreach (Collider2D theirs in others)
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
        AnimatorBackOnGameTime();
        Vector2 look = facingAtEnd != Vector2.zero ? facingAtEnd : heading;
        FlipSpriteToFace.Apply(transform, look);
        CharacterWalkAnimation.Apply(animator, false, look);
        if (TryGetComponent(out NpcCharacter npc))
            ExplorationPlayerSession.SaveNpcPosition(npc.PersistenceKey, new Vector3(position.x, position.y, transform.position.z));
    }
}
