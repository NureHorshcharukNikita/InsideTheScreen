using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rigidBody;
    private Animator animator;
    private Vector2 movement;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!GameStateManager.IsGameplay)
        {
            StopMovement();
            return;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        movement = new Vector2(moveX, moveY).normalized;

        UpdateAnimation(moveX, moveY);
    }

    private void FixedUpdate()
    {
        rigidBody.MovePosition(rigidBody.position + movement * speed * Time.fixedDeltaTime);
    }

    public void StopMovement()
    {
        movement = Vector2.zero;
        rigidBody.linearVelocity = Vector2.zero;
        animator.Play("Idle_Down");
    }

    private void UpdateAnimation(float moveX, float moveY)
    {
        if (movement.magnitude > 0)
        {
            if (moveY > 0)
            {
                animator.Play("Walk_Up");
            }
            else if (moveY < 0)
            {
                animator.Play("Walk_Down");
            }
            else if (moveX < 0)
            {
                animator.Play("Walk_Left");
            }
            else if (moveX > 0)
            {
                animator.Play("Walk_Right");
            }
        }
        else
        {
            animator.Play("Idle_Down");
        }
    }
}