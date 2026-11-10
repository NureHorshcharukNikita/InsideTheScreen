using System.Collections;
using UnityEngine;

// A character left standing when a cutscene or a talk is over (a dialogue, or the shop after it)
// turns to face ahead (down), e.g. Garr turned towards Haru while they talked. Only once the
// scenes are over: when one starts right after another (the forest wake-up after the home intro),
// everyone keeps looking where the last one left them, e.g. Hideshi at Haru. Not a companion
// walking with Haru: standing by him, he keeps looking at him (CompanionFollower). Waiting on his
// own, he turns ahead like anyone. Nothing in between: during play nobody is turned.
public sealed class StandingFacesAhead : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int DirectionParameter = Animator.StringToHash("Direction");
    // How long play has to go on after a scene before he turns. A scene that follows at once starts
    // within a frame or two (an Automatically event whose flag the last one set), so it finds him
    // still turned the way he was.
    private const float TurnAfterSeconds = 0.3f;

    private SpriteRenderer look;
    private Animator animator;
    private RuntimeAnimatorController parametersOf;
    private bool hasIsMoving;
    private bool hasDirection;
    private CompanionFollower companion;
    // The game state last seen, to tell a cutscene or a talk ending from, e.g., the pause menu closing.
    private GameState seen;
    private Coroutine turning;

    // Added by NpcCharacter. Only with an Animator to turn it: drawn as one sprite, nothing is added.
    public static void Attach(GameObject target)
    {
        if (target == null || target.GetComponentInChildren<Animator>() == null
            || target.TryGetComponent(out StandingFacesAhead _))
            return;

        target.AddComponent<StandingFacesAhead>();
    }

    private void Awake()
    {
        look = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        companion = GetComponent<CompanionFollower>();
    }

    private void OnEnable()
    {
        seen = GameStateManager.State;
        GameStateManager.StateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        GameStateManager.StateChanged -= OnStateChanged;
        turning = null;
    }

    private void OnStateChanged()
    {
        GameState was = seen;
        seen = GameStateManager.State;
        if (IsScene(seen))
            StopTurning();
        else if (seen == GameState.Gameplay && IsScene(was))
        {
            StopTurning();
            turning = StartCoroutine(TurnAheadOnceOver());
        }
    }

    // Game time: it stands still while the game is paused or the inventory is open.
    private IEnumerator TurnAheadOnceOver()
    {
        yield return new WaitForSeconds(TurnAfterSeconds);
        turning = null;
        if (GameStateManager.IsGameplay && StandsTurnedAway())
            CharacterWalkAnimation.Apply(animator, false, Vector2.down);
    }

    private void StopTurning()
    {
        if (turning != null)
            StopCoroutine(turning);
        turning = null;
    }

    // A cutscene (a story event plays in the Dialogue state), a talk, or the shop after one.
    private static bool IsScene(GameState state)
    {
        return state == GameState.Dialogue || state == GameState.Shop;
    }

    // Shown, left to itself, not walking, and facing another way than down (CharacterWalkAnimation:
    // Direction 0 is down).
    private bool StandsTurnedAway()
    {
        if (look == null || !look.enabled || StoryEventContext.IsControlledByStory(transform)
            || TryGetComponent(out BackgroundWalk _) || (companion != null && companion.IsFollowing)
            || !HasAnimator() || !hasDirection)
            return false;

        return !(hasIsMoving && animator.GetBool(IsMovingParameter)) && animator.GetInteger(DirectionParameter) != 0;
    }

    private bool HasAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            return false;

        if (parametersOf != animator.runtimeAnimatorController)
        {
            parametersOf = animator.runtimeAnimatorController;
            hasIsMoving = false;
            hasDirection = false;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                hasIsMoving |= parameter.nameHash == IsMovingParameter;
                hasDirection |= parameter.nameHash == DirectionParameter;
            }
        }

        return true;
    }
}
