using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    private const float MovementThreshold = 0.001f;

    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int DirectionParameter = Animator.StringToHash("Direction");

    public float speed = 5f;

    private Rigidbody2D rigidBody;
    private Animator animator;
    private Vector2 movement;
    private PlayerFacingDirection facingDirection = PlayerFacingDirection.Down;

    public bool IsMoving => GameStateManager.IsGameplay && movement.sqrMagnitude > MovementThreshold;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (ExplorationPlayerSession.TryGetSavedPosition(out Vector3 savedPosition))
            transform.position = savedPosition;
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplay)
        {
            StopMovement();
            return;
        }

        movement = GameInputSettings.GetMovement();
        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!GameStateManager.IsGameplay)
            return;

        rigidBody.MovePosition(rigidBody.position + movement * speed * Time.fixedDeltaTime);
    }

    public void StopMovement()
    {
        movement = Vector2.zero;
        rigidBody.linearVelocity = Vector2.zero;
        animator.SetBool(IsMovingParameter, false);
    }

    private void UpdateAnimation()
    {
        bool isMoving = movement.sqrMagnitude > MovementThreshold;
        animator.SetBool(IsMovingParameter, isMoving);

        if (!isMoving)
            return;

        if (Mathf.Abs(movement.y) >= Mathf.Abs(movement.x))
        {
            facingDirection = movement.y > 0f ? PlayerFacingDirection.Up : PlayerFacingDirection.Down;
        }
        else
        {
            facingDirection = movement.x > 0f ? PlayerFacingDirection.Right : PlayerFacingDirection.Left;
        }

        animator.SetInteger(DirectionParameter, (int)facingDirection);
    }

    private enum PlayerFacingDirection
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3
    }
}
