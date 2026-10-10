using UnityEngine;

// Sets the IsMoving / Direction parameters the player's animator uses, for characters moved
// by code (story events, companions). Does nothing until the character has such an animator.
public static class CharacterWalkAnimation
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int DirectionParameter = Animator.StringToHash("Direction");

    public static void Apply(Animator animator, bool moving, Vector2 direction)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == IsMovingParameter)
                animator.SetBool(IsMovingParameter, moving);
            else if (parameter.nameHash == DirectionParameter && direction.sqrMagnitude > 0.0001f)
                animator.SetInteger(DirectionParameter, GetDirection(direction));
        }
    }

    // Same values as PlayerMovement: 0 down, 1 up, 2 left, 3 right.
    private static int GetDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
            return direction.y > 0f ? 1 : 0;

        return direction.x > 0f ? 3 : 2;
    }
}
