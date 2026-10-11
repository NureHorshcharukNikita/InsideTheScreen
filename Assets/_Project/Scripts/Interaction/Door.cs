using System.Collections;
using UnityEngine;

// Swings open when the player walks into the trigger and closes once they leave,
// flipping through sprite frames from fully closed to fully open.
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class Door : MonoBehaviour
{
    [Tooltip("Frames from fully closed to fully open.")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameDuration = 0.08f;
    [Tooltip("Optional solid collider that blocks the doorway while the door is closed.")]
    [SerializeField] private Collider2D blocker;

    private SpriteRenderer spriteRenderer;
    private Coroutine swingRoutine;
    private int currentFrame;
    private bool playerNearby;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"Door {name} has no frames.", this);
            enabled = false;
            return;
        }

        currentFrame = 0;
        spriteRenderer.sprite = frames[0];
        SetBlocking(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playerNearby || !other.CompareTag("Player"))
            return;

        playerNearby = true;
        SwingTo(frames.Length - 1);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!playerNearby || !other.CompareTag("Player"))
            return;

        playerNearby = false;
        SwingTo(0);
    }

    private void SwingTo(int targetFrame)
    {
        if (!isActiveAndEnabled)
            return;

        if (swingRoutine != null)
            StopCoroutine(swingRoutine);

        // Let the player through as soon as the door starts opening; block again once it starts closing.
        SetBlocking(targetFrame == 0);
        swingRoutine = StartCoroutine(Swing(targetFrame));
    }

    private IEnumerator Swing(int targetFrame)
    {
        int step = targetFrame > currentFrame ? 1 : -1;
        while (currentFrame != targetFrame)
        {
            yield return new WaitForSeconds(frameDuration);
            currentFrame += step;
            spriteRenderer.sprite = frames[currentFrame];
        }

        swingRoutine = null;
    }

    private void SetBlocking(bool blocking)
    {
        if (blocker != null)
            blocker.enabled = blocking;
    }
}
