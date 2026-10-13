using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Makes an enemy on the map notice the player and run after them, then walk back to its spot once
// the player gets away. It notices a player it can see within Sight Radius who is on its ground:
// within Leash Radius of its spot. A guard has a small ground and defends only its place; a hunter
// roams far. Chase Speed decides who can be outrun: the player walks at 5. Running into the player
// starts the battle (EnemyEncounter). On the way back it takes the shortest way it knows: straight
// to its spot when nothing is in the way, otherwise cutting the corners of the way it came.
[DisallowMultipleComponent]
public sealed class EnemyChaser : MonoBehaviour
{
    // Back from a battle the player stands next to the enemies again: a moment to get away first.
    private const float CalmAfterSceneLoad = 2f;
    private const float TrailSpacing = 0.5f;
    private const float ArriveDistance = 0.15f;
    private const float StuckGiveUpTime = 1f;
    // On the way back, how often it looks again for a shorter way, as new shortcuts open up.
    private const float ReturnRepathInterval = 0.3f;
    private const float FacingThreshold = 0.2f;

    [Tooltip("How far from its edge it sees the player. Walls block its view.")]
    [SerializeField, Min(0f)] private float sightRadius = 4f;
    [Tooltip("How far from its spot it goes after the player; it only cares about a player this close to its spot. Small for a guard.")]
    [SerializeField, Min(0f)] private float leashRadius = 12f;
    [Tooltip("The player walks at 5: slower enemies can be outrun, faster ones cannot.")]
    [SerializeField, Min(0.1f)] private float chaseSpeed = 4f;
    [SerializeField, Min(0.1f)] private float returnSpeed = 3f;
    [Tooltip("How long it stands with a \"!\" over its head before it runs, so the player can react.")]
    [SerializeField, Min(0f)] private float noticeDelay = 0.4f;
    [Tooltip("How long it looks for a player it has lost sight of before it walks back.")]
    [SerializeField, Min(0f)] private float giveUpTime = 1.5f;
    [Tooltip("Which way the map sprite looks; it is flipped to look where it runs.")]
    [SerializeField] private bool spriteFacesLeft = true;

    private readonly List<Vector2> trail = new();
    private readonly List<Collider2D> ownColliders = new();
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;
    private Collider2D feet;
    private TMP_Text alertMark;
    private ContactFilter2D obstacleFilter;
    private PlayerMovement player;
    private Collider2D playerCollider;
    private Vector2 home;
    private State state = State.Idle;
    private float stateTime;
    private Vector2 lastSeenPlayer;
    private float lostSightTime;
    private Vector2 lastBodyPosition;
    private float commandedStep;
    private float stuckTime;
    private float facingX;
    private Vector2 returnWaypoint;
    private float nextReturnRepathTime;

    private enum State
    {
        Idle,
        Noticing,
        Chasing,
        Returning
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponents(ownColliders);
        feet = ownColliders.Find(part => !part.isTrigger);

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

        home = body.position;
        lastBodyPosition = home;
    }

    private void Update()
    {
        ShowAlertMark(state == State.Noticing);

        if (spriteRenderer != null && Mathf.Abs(facingX) >= FacingThreshold)
            spriteRenderer.flipX = spriteFacesLeft ? facingX > 0f : facingX < 0f;
    }

    private void FixedUpdate()
    {
        if (!TryFindPlayer())
            return;

        // Dialogue, menus and story events hold it where it is; so does a battle about to start.
        if (!GameStateManager.IsGameplay || !player.enabled)
        {
            Halt();
            return;
        }

        TrackStuck();
        stateTime += Time.fixedDeltaTime;

        switch (state)
        {
            case State.Idle:
                if (CanNoticePlayer())
                    SetState(State.Noticing);
                break;

            case State.Noticing:
                facingX = PlayerFeetCenter.x - FeetCenter.x;
                if (stateTime >= noticeDelay)
                    SetState(State.Chasing);
                break;

            case State.Chasing:
                Chase();
                break;

            case State.Returning:
                if (CanNoticePlayer())
                    SetState(State.Noticing);
                else
                    Return();
                break;
        }
    }

    private Vector2 FeetCenter => feet != null ? (Vector2)feet.bounds.center : body.position;

    private Vector2 PlayerFeetCenter => playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)player.transform.position;

    private bool CanNoticePlayer()
    {
        if (Time.timeSinceLevelLoad < CalmAfterSceneLoad)
            return false;

        // From its edge, not its middle, so a big enemy does not see further than a small one.
        Vector2 playerFeet = PlayerFeetCenter;
        Vector2 nearestPart = feet != null ? feet.ClosestPoint(playerFeet) : body.position;
        return Vector2.Distance(nearestPart, playerFeet) <= sightRadius
            && Vector2.Distance(home, playerFeet) <= leashRadius
            && CanSee(playerFeet);
    }

    private void Chase()
    {
        if (Vector2.Distance(body.position, home) > leashRadius || stuckTime >= StuckGiveUpTime)
        {
            SetState(State.Returning);
            return;
        }

        Vector2 playerFeet = PlayerFeetCenter;
        if (CanSee(playerFeet))
        {
            lastSeenPlayer = playerFeet;
            lostSightTime = 0f;
        }
        else
        {
            // Out of sight: run to where the player was last seen and look around a little.
            lostSightTime += Time.fixedDeltaTime;
            if (lostSightTime >= giveUpTime)
            {
                SetState(State.Returning);
                return;
            }
        }

        if ((body.position - (trail.Count > 0 ? trail[^1] : home)).sqrMagnitude >= TrailSpacing * TrailSpacing)
            trail.Add(body.position);

        Step(lastSeenPlayer - FeetCenter, chaseSpeed);
    }

    // Heads for returnWaypoint: its spot once the trail is empty, otherwise the trail's last point.
    private void Return()
    {
        bool arrived = (returnWaypoint - body.position).sqrMagnitude <= ArriveDistance * ArriveDistance;
        if (arrived)
        {
            if (trail.Count == 0)
            {
                SetState(State.Idle);
                return;
            }

            trail.RemoveAt(trail.Count - 1);
        }

        if (arrived || Time.time >= nextReturnRepathTime)
            PickReturnWaypoint();

        // The way back is one it has walked, so this only happens when something has moved in the way.
        if (stuckTime >= StuckGiveUpTime * 2f)
        {
            body.position = home;
            SetState(State.Idle);
            return;
        }

        Step(returnWaypoint - body.position, returnSpeed);
    }

    // Its spot if it can walk there in a straight line, otherwise the point of the way it came
    // closest to its spot that it can: the rest of the way, past that point, is a detour.
    private void PickReturnWaypoint()
    {
        nextReturnRepathTime = Time.time + ReturnRepathInterval;

        if (CanWalkStraightTo(home))
        {
            trail.Clear();
            returnWaypoint = home;
            return;
        }

        for (int i = 0; i < trail.Count - 1; i++)
        {
            if (CanWalkStraightTo(trail[i]))
            {
                trail.RemoveRange(i + 1, trail.Count - i - 1);
                break;
            }
        }

        returnWaypoint = trail.Count > 0 ? trail[^1] : home;
    }

    private void Step(Vector2 toTarget, float speed)
    {
        float distance = toTarget.magnitude;
        if (distance <= ArriveDistance)
        {
            Halt();
            return;
        }

        Vector2 direction = toTarget / distance;
        if (Mathf.Abs(direction.x) >= FacingThreshold)
            facingX = direction.x;

        commandedStep = Mathf.Min(speed * Time.fixedDeltaTime, distance);
        body.linearVelocity = Vector2.zero;
        body.MovePosition(body.position + direction * commandedStep);
    }

    private void Halt()
    {
        commandedStep = 0f;
        body.linearVelocity = Vector2.zero;
    }

    // Compares the last physics step with the step asked for: pressed against a wall, it gives up.
    private void TrackStuck()
    {
        float moved = Vector2.Distance(body.position, lastBodyPosition);
        lastBodyPosition = body.position;

        if (commandedStep <= 0f || moved >= commandedStep * 0.1f)
            stuckTime = 0f;
        else
            stuckTime += Time.fixedDeltaTime;
    }

    // Moving it walls stop it; standing nobody pushes it around.
    private void SetState(State next)
    {
        state = next;
        stateTime = 0f;
        stuckTime = 0f;
        Halt();

        bool moving = next == State.Chasing || next == State.Returning;
        body.bodyType = moving ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        body.linearVelocity = Vector2.zero;

        if (next == State.Chasing)
        {
            lastSeenPlayer = PlayerFeetCenter;
            lostSightTime = 0f;
        }
        else if (next == State.Returning)
        {
            PickReturnWaypoint();
        }
        else if (next == State.Idle)
        {
            trail.Clear();
        }
    }

    private bool CanSee(Vector2 target)
    {
        int count = Physics2D.Linecast(FeetCenter, target, obstacleFilter, castHits);
        return !HitsObstacle(count);
    }

    // bodyTarget: where its body (not its feet) is to stand.
    private bool CanWalkStraightTo(Vector2 bodyTarget)
    {
        Vector2 delta = bodyTarget - body.position;
        float distance = delta.magnitude;
        if (distance < 0.0001f)
            return true;

        Vector2 size = feet != null ? (Vector2)feet.bounds.size * 0.95f : Vector2.one * 0.5f;
        int count = Physics2D.BoxCast(FeetCenter, size, 0f, delta / distance, obstacleFilter, castHits, distance);
        return !HitsObstacle(count);
    }

    // Walls are obstacles; people are not, the player's companions and other enemies included.
    private bool HitsObstacle(int hitCount)
    {
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = castHits[i].collider;
            if (hit == null || ownColliders.Contains(hit))
                continue;

            Rigidbody2D hitBody = hit.attachedRigidbody;
            if (hitBody != null && hitBody.bodyType != RigidbodyType2D.Static)
                continue;

            return true;
        }

        return false;
    }

    private bool TryFindPlayer()
    {
        if (player != null)
            return true;

        player = FindFirstObjectByType<PlayerMovement>();
        if (player == null)
            return false;

        foreach (Collider2D part in player.GetComponents<Collider2D>())
        {
            if (!part.isTrigger)
            {
                playerCollider = part;
                break;
            }
        }

        return true;
    }

    private void ShowAlertMark(bool show)
    {
        if (alertMark == null)
        {
            if (!show)
                return;

            alertMark = AlertMark.Create(transform);
        }

        if (alertMark.gameObject.activeSelf != show)
            alertMark.gameObject.SetActive(show);
    }
}
