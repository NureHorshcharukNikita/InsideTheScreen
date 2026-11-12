using UnityEngine;

// How a character looks until their profile's Look Changes At flag is set (CharacterProfile), e.g.
// Haru without his bags until Garr gives them: on the map another Animator Controller (his walk
// without them), in battle another sprite. It switches the moment the flag is set, facing the way
// they faced. Empty fields change nothing.
public sealed class StoryLook : MonoBehaviour
{
    private static readonly int DirectionParameter = Animator.StringToHash("Direction");
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");

    [Tooltip("Whose Look Changes At flag it follows, e.g. HaruProfile.")]
    [SerializeField] private CharacterProfile profile;
    [Tooltip("Played until then where the character walks, e.g. an override of Player.controller with the walk without bags.")]
    [SerializeField] private RuntimeAnimatorController animatorBefore;
    [Tooltip("Shown until then where the character is drawn as one sprite, e.g. Haru's picture in battle.")]
    [SerializeField] private Sprite spriteBefore;

    private Animator animator;
    private SpriteRenderer look;
    private RuntimeAnimatorController animatorAfter;
    private Sprite spriteAfter;
    private bool applied;
    private bool lookingAsBefore;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        look = GetComponentInChildren<SpriteRenderer>();
        animatorAfter = animator != null ? animator.runtimeAnimatorController : null;
        spriteAfter = look != null ? look.sprite : null;
    }

    private void OnEnable()
    {
        StoryFlagManager.Changed += Apply;
        Apply();
    }

    private void OnDisable()
    {
        StoryFlagManager.Changed -= Apply;
    }

    private void Apply()
    {
        bool before = profile != null && profile.LooksAsBefore;
        if (applied && before == lookingAsBefore)
            return;

        applied = true;
        lookingAsBefore = before;
        if (animator != null && animatorBefore != null && animatorAfter != null)
            SwapController(before ? animatorBefore : animatorAfter);

        if (look != null && spriteBefore != null)
            look.sprite = before ? spriteBefore : spriteAfter;
    }

    // A new controller starts from its defaults: the way they face and walk is carried over.
    private void SwapController(RuntimeAnimatorController controller)
    {
        if (animator.runtimeAnimatorController == controller)
            return;

        bool hasDirection = HasParameter(DirectionParameter);
        bool hasIsMoving = HasParameter(IsMovingParameter);
        int direction = hasDirection ? animator.GetInteger(DirectionParameter) : 0;
        bool moving = hasIsMoving && animator.GetBool(IsMovingParameter);

        animator.runtimeAnimatorController = controller;
        if (hasDirection && HasParameter(DirectionParameter))
            animator.SetInteger(DirectionParameter, direction);
        if (hasIsMoving && HasParameter(IsMovingParameter))
            animator.SetBool(IsMovingParameter, moving);
        if (animator.isActiveAndEnabled)
            animator.Update(0f);
    }

    private bool HasParameter(int nameHash)
    {
        if (animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == nameHash)
                return true;
        }

        return false;
    }
}
