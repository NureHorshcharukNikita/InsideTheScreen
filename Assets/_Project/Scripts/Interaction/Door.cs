using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Swings open by flipping through sprite frames from fully closed to fully open, and closes once
// everyone has left its trigger area. Either opens by itself when someone walks up, or waits for
// Open() (e.g. a doorway asking the player to press Interact first). A character a story event is
// walking opens it either way: they cannot press Interact.
// Someone is the player or any NPC, the companion included. Checked by position every frame rather
// than with trigger events, which need physics to step: story events move characters by their
// transforms while the game is paused (Time.timeScale 0).
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class Door : MonoBehaviour
{
    [Tooltip("Frames from fully closed to fully open.")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameDuration = 0.08f;
    [Tooltip("Optional collider over the doorway: solid while the door is closed, passable while it is open.")]
    [SerializeField] private Collider2D blocker;
    [Tooltip("Open as soon as the player or an NPC walks up. Turn off to open only through Open().")]
    [SerializeField] private bool opensAutomatically = true;
    [Tooltip("How far (in units) the player's center must get past the doorway's near edge before walking through counts.")]
    [SerializeField] private float walkThroughDepth = 1f;

    // Raised once per opening when the player steps deep enough into the open doorway (the blocker area).
    public event Action<Transform> PlayerWalkedThrough;

    public bool IsOpen { get; private set; }
    public bool OpensAutomatically => opensAutomatically;
    public bool IsSwinging => swingRoutine != null;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D area;
    private Coroutine swingRoutine;
    private int currentFrame;
    private Collider2D playerBody;
    private readonly List<Collider2D> npcBodies = new();
    private Collider2D nearbyPlayer;
    private bool someoneWasNear;
    // Shut on purpose (Shut()): stays closed until everyone has stepped away.
    private bool heldShut;
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
        area = GetComponent<BoxCollider2D>();
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

    private void Start()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
            playerBody = GetBody(player.gameObject);

        // Inactive ones too: some appear later in the story.
        foreach (NpcCharacter npc in FindObjectsByType<NpcCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Collider2D body = GetBody(npc.gameObject);
            if (body != null)
                npcBodies.Add(body);
        }
    }

    private void Update()
    {
        Bounds doorway = Flatten(area.bounds);
        nearbyPlayer = IsInside(playerBody, doorway) ? playerBody : null;
        bool someoneNear = nearbyPlayer != null;
        bool someoneOpens = someoneNear && OpensFor(nearbyPlayer);
        for (int i = 0; i < npcBodies.Count; i++)
        {
            if (!IsInside(npcBodies[i], doorway))
                continue;

            someoneNear = true;
            someoneOpens |= OpensFor(npcBodies[i]);
        }

        if (!someoneNear)
            heldShut = false;

        if (someoneOpens && !heldShut)
            Open();
        else if (!someoneNear && someoneWasNear)
            Close();
        someoneWasNear = someoneNear;

        TrackWalkThrough();
    }

    // Closed by someone on purpose, e.g. Hideshi shutting the door behind him in a story event
    // (DoorStep): it stays shut while they stand next to it, and opens for people again once
    // everyone has stepped away.
    public void Shut()
    {
        heldShut = true;
        Close();
    }

    private bool OpensFor(Collider2D body)
    {
        return opensAutomatically || StoryEventContext.IsControlledByStory(body.transform);
    }

    private void TrackWalkThrough()
    {
        if (!IsOpen || walkThroughReported || nearbyPlayer == null || blocker == null)
            return;

        // Measure along the doorway's thin axis: x for side doors, y for front doors.
        Bounds doorway = blocker.bounds;
        int axis = doorway.size.x <= doorway.size.y ? 0 : 1;
        float playerPosition = GetBodyBounds(nearbyPlayer).center[axis];
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

    // On the GameClock: it also swings during story events, while the game is paused.
    private IEnumerator Swing(int targetFrame)
    {
        int step = targetFrame > currentFrame ? 1 : -1;
        while (currentFrame != targetFrame)
        {
            yield return new WaitForGameSeconds(frameDuration);
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

    private static bool IsInside(Collider2D body, Bounds doorway)
    {
        return body != null && body.isActiveAndEnabled && GetBodyBounds(body).Intersects(doorway);
    }

    // The collider a character stands on: the solid one, not a talk zone.
    private static Collider2D GetBody(GameObject character)
    {
        Collider2D fallback = null;
        foreach (Collider2D collider in character.GetComponents<Collider2D>())
        {
            if (!collider.isTrigger)
                return collider;

            fallback ??= collider;
        }

        return fallback;
    }

    // From the transform, not the physics shape: that only follows the transform when physics steps.
    private static Bounds GetBodyBounds(Collider2D body)
    {
        Transform bodyTransform = body.transform;
        Vector3 center = bodyTransform.TransformPoint(body.offset);
        Vector3 size = body is BoxCollider2D box
            ? new Vector3(box.size.x * Mathf.Abs(bodyTransform.lossyScale.x), box.size.y * Mathf.Abs(bodyTransform.lossyScale.y), 0f)
            : body.bounds.size;
        return Flatten(new Bounds(center, size));
    }

    // Compared in 2D: depth would keep sprites at different z from ever overlapping.
    private static Bounds Flatten(Bounds bounds)
    {
        return new Bounds(new Vector3(bounds.center.x, bounds.center.y, 0f), new Vector3(bounds.size.x, bounds.size.y, 1f));
    }
}
