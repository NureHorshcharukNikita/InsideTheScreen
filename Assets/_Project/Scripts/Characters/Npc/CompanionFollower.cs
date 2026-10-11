using UnityEngine;

// Makes an NPC walk after the player once a story flag is set, e.g. Hideshi travelling with
// Haru through Arc 1. There is no pathfinding: the companion walks straight to the player and
// jumps next to them when left far behind (doorways, story teleports).
[RequireComponent(typeof(NpcCharacter))]
public sealed class CompanionFollower : MonoBehaviour
{
    [SerializeField, StoryFlag] private string followAfterStoryFlag = "a1_home_intro";
    [Tooltip("Keep it just outside the NPC talk zone, so the Talk prompt is not shown all the time.")]
    [SerializeField, Min(0.1f)] private float followDistance = 2.8f;
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField, Min(1f)] private float teleportDistance = 12f;

    private NpcCharacter npc;
    private Animator animator;
    private Collider2D bodyCollider;
    private Transform player;
    private Vector2 lastPlayerPosition;
    private Vector2 playerHeading = Vector2.down;
    private bool following;

    public bool IsFollowing => following;

    private void Awake()
    {
        npc = GetComponent<NpcCharacter>();
        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        SetFollowing(StoryFlagManager.HasFlag(followAfterStoryFlag));
        if (!following || !TryFindPlayer())
            return;

        if (!GameStateManager.IsGameplay)
        {
            CharacterWalkAnimation.Apply(animator, false, Vector2.zero);
            return;
        }

        Vector2 playerPosition = player.position;
        Vector2 playerStep = playerPosition - lastPlayerPosition;
        if (playerStep.sqrMagnitude > 0.0001f)
            playerHeading = playerStep.normalized;
        lastPlayerPosition = playerPosition;

        Vector2 toPlayer = playerPosition - (Vector2)transform.position;
        float distance = toPlayer.magnitude;

        if (distance > teleportDistance)
        {
            npc.TeleportTo(playerPosition - playerHeading * followDistance);
            CharacterWalkAnimation.Apply(animator, false, playerHeading);
            return;
        }

        if (distance <= followDistance)
        {
            CharacterWalkAnimation.Apply(animator, false, toPlayer);
            return;
        }

        // Catch up a little faster when falling behind.
        float speed = distance > followDistance * 2f ? moveSpeed * 1.5f : moveSpeed;
        Vector2 direction = toPlayer / distance;
        Vector2 step = direction * Mathf.Min(speed * Time.deltaTime, distance - followDistance);
        transform.position += (Vector3)step;
        CharacterWalkAnimation.Apply(animator, true, direction);
    }

    private bool TryFindPlayer()
    {
        if (player != null)
            return true;

        PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
        if (movement == null)
            return false;

        player = movement.transform;
        lastPlayerPosition = player.position;
        return true;
    }

    private void SetFollowing(bool value)
    {
        if (following == value)
            return;

        following = value;

        // A companion walking at the player's heels must not block them in narrow passages.
        if (bodyCollider != null)
            bodyCollider.isTrigger = value;
    }
}
