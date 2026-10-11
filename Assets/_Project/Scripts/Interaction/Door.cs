using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Swings open by flipping through sprite frames from fully closed to fully open, and closes
// once the player and a following companion have left the trigger. Either opens by itself when
// one of them walks up, or waits for Open() (e.g. a doorway asking the player to press Interact first).
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class Door : MonoBehaviour
{
    [Tooltip("Frames from fully closed to fully open.")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameDuration = 0.08f;
    [Tooltip("Optional collider over the doorway: solid while the door is closed, passable while it is open.")]
    [SerializeField] private Collider2D blocker;
    [Tooltip("Open as soon as the player walks up. Turn off to open only through Open().")]
    [SerializeField] private bool opensAutomatically = true;
    [Tooltip("How far (in units) the player's center must get past the doorway's near edge before walking through counts.")]
    [SerializeField] private float walkThroughDepth = 1f;

    // Raised once per opening when the player steps deep enough into the open doorway (the blocker area).
    public event Action<Transform> PlayerWalkedThrough;

    public bool IsOpen { get; private set; }
    public bool OpensAutomatically => opensAutomatically;

    private SpriteRenderer spriteRenderer;
    private Coroutine swingRoutine;
    private int currentFrame;
    private readonly HashSet<Collider2D> visitors = new();
    private Collider2D nearbyPlayer;
    private bool walkThroughReported;
    // Which side of the doorway the player approached from (+1/-1 along its thin axis); 0 until known.
    private int entrySide;

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
        SetPassable(false);
    }

    private void Update()
    {
        if (!IsOpen || walkThroughReported || nearbyPlayer == null || blocker == null)
            return;

        // Measure along the doorway's thin axis: x for side doors, y for front doors.
        Bounds doorway = blocker.bounds;
        int axis = doorway.size.x <= doorway.size.y ? 0 : 1;
        float playerPosition = nearbyPlayer.bounds.center[axis];
        if (entrySide == 0)
            entrySide = playerPosition >= doorway.center[axis] ? 1 : -1;

        float nearEdge = doorway.center[axis] + entrySide * doorway.extents[axis];
        float depth = (nearEdge - playerPosition) * entrySide;
        if (depth >= walkThroughDepth)
        {
            walkThroughReported = true;
            PlayerWalkedThrough?.Invoke(nearbyPlayer.transform);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isPlayer = other.TryGetComponent(out PlayerMovement _);
        bool isCompanion = other.TryGetComponent(out CompanionFollower companion) && companion.IsFollowing;
        if ((!isPlayer && !isCompanion) || !visitors.Add(other))
            return;

        if (isPlayer)
            nearbyPlayer = other;

        if (opensAutomatically)
            Open();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!visitors.Remove(other))
            return;

        if (other == nearbyPlayer)
            nearbyPlayer = null;

        if (visitors.Count == 0)
            Close();
    }

    public void Open()
    {
        if (!IsOpen)
            SwingTo(frames.Length - 1);
    }

    public void Close()
    {
        if (IsOpen)
            SwingTo(0);
    }

    private void SwingTo(int targetFrame)
    {
        if (!isActiveAndEnabled)
            return;

        if (swingRoutine != null)
            StopCoroutine(swingRoutine);

        // Let the player through as soon as the door starts opening; block again once it starts closing.
        IsOpen = targetFrame != 0;
        walkThroughReported = false;
        entrySide = 0;
        SetPassable(IsOpen);
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

    private void SetPassable(bool passable)
    {
        if (blocker != null)
            blocker.isTrigger = passable;
    }
}
