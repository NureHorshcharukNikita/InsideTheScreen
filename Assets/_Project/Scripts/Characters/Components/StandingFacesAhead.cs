using UnityEngine;

// A character left standing when a cutscene or a talk is over (a dialogue, or the shop after it)
// turns at once to face ahead (down), e.g. Garr turned towards Haru while they talked. Not a
// companion walking with Haru: standing by him, he keeps looking at him (CompanionFollower).
// Waiting on his own, he turns ahead like anyone. Nothing in between: during play nobody is
// turned.
public sealed class StandingFacesAhead : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int DirectionParameter = Animator.StringToHash("Direction");

    private SpriteRenderer look;
    private Animator animator;
    private RuntimeAnimatorController parametersOf;
    private bool hasIsMoving;
    private bool hasDirection;
    private CompanionFollower companion;
    // The game state last seen, to tell a cutscene or a talk ending from, e.g., the pause menu closing.
    private GameState seen;

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
    }

    // A story event lets go of its actors before play comes back, so by now nobody is held.
    private void OnStateChanged()
    {
        GameState was = seen;
        seen = GameStateManager.State;
        if (seen == GameState.Gameplay && (was == GameState.Dialogue || was == GameState.Shop) && StandsTurnedAway())
            CharacterWalkAnimation.Apply(animator, false, Vector2.down);
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
