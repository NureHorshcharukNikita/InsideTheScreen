using TMPro;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class HouseDoorway : MonoBehaviour
{
    [SerializeField] private HouseDoorway destination;
    [SerializeField] private Transform arrivalPoint;
    [Tooltip("Camera bounds of the area this doorway is in.")]
    [SerializeField] private Collider2D cameraBounds;
    [SerializeField] private GameObject minimapRoot;
    [SerializeField] private bool hideMinimap;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string promptAction = "Enter";
    [Tooltip("Optional door: Interact opens it, and walking through the open doorway uses this doorway.")]
    [SerializeField] private Door door;
    [SerializeField] private string openPromptAction = "Open";

    private static bool transitionInProgress;
    private PlayerMovement nearbyPlayer;
    private InteractionPrompt prompt;

    private void Awake()
    {
        if (promptRoot != null)
        {
            if (promptCanvasGroup == null)
                promptCanvasGroup = promptRoot.GetComponent<CanvasGroup>();
            if (promptText == null)
                promptText = promptRoot.GetComponentInChildren<TMP_Text>(true);
        }

        if (promptRoot == null || promptCanvasGroup == null || promptText == null)
            Debug.LogWarning($"House doorway {name} has incomplete prompt references.", this);

        SetPromptVisible(false);
    }

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnEnable()
    {
        if (door != null)
            door.PlayerWalkedThrough += OnPlayerWalkedThroughDoor;
    }

    private void OnDisable()
    {
        if (door != null)
            door.PlayerWalkedThrough -= OnPlayerWalkedThroughDoor;
    }

    private void Start()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null && cameraBounds != null && cameraBounds.OverlapPoint(player.transform.position))
        {
            PlayerTransfer.SetCameraBounds(cameraBounds);
            SetMinimapVisibility();
        }
    }

    private void Update()
    {
        bool showPrompt = nearbyPlayer != null && GameStateManager.IsGameplay && !transitionInProgress
            && (door == null || !door.IsOpen);
        if (showPrompt && !PromptShown)
            RefreshPrompt();
        SetPromptVisible(showPrompt);

        if (!showPrompt || !GameInputSettings.GetKeyDown(GameInputAction.Interact))
            return;

        if (door != null)
            door.Open();
        else
            EnterHouse();
    }

    private void OnPlayerWalkedThroughDoor(Transform player)
    {
        if (nearbyPlayer == null && player.TryGetComponent(out PlayerMovement movement))
            nearbyPlayer = movement;

        EnterHouse();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TrackNearbyPlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TrackNearbyPlayer(other);
    }

    private void TrackNearbyPlayer(Collider2D other)
    {
        if (nearbyPlayer != null || !other.CompareTag("Player") || !other.TryGetComponent(out PlayerMovement movement))
            return;

        nearbyPlayer = movement;
        RefreshPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement>() != nearbyPlayer)
            return;

        nearbyPlayer = null;
        SetPromptVisible(false);
    }

    private void EnterHouse()
    {
        if (!GameStateManager.IsGameplay || transitionInProgress || nearbyPlayer == null)
            return;

        if (destination == null || destination.arrivalPoint == null || destination.cameraBounds == null)
        {
            Debug.LogWarning($"House doorway {name} is missing its destination or camera bounds.", this);
            return;
        }

        PlayerMovement movement = nearbyPlayer;

        transitionInProgress = true;
        SetPromptVisible(false);
        movement.StopMovement();
        movement.enabled = false;

        if (!FadeManager.TryFadeWithinScene(
            () => TransferPlayer(movement.transform),
            () =>
            {
                if (movement != null)
                    movement.enabled = true;
                transitionInProgress = false;
            }))
        {
            movement.enabled = true;
            transitionInProgress = false;
        }
    }

    private void RefreshPrompt()
    {
        string keyName = GameInputSettings.GetDisplayName(GameInputAction.Interact);
        string action = door != null ? openPromptAction : promptAction;
        if (Prompt != null)
            Prompt.ShowAction(keyName, action);
        else if (promptText != null)
            promptText.text = $"{keyName} - {action}";
    }

    private InteractionPrompt Prompt => prompt != null ? prompt : prompt = InteractionPrompt.For(promptText);

    // Shown, or on its way in.
    private bool PromptShown => Prompt != null ? Prompt.IsShown : promptRoot != null && promptRoot.activeSelf;

    // The prompt plays its way in and out itself (InteractionPrompt).
    private void SetPromptVisible(bool visible)
    {
        if (Prompt != null)
        {
            Prompt.SetShown(visible);
            return;
        }

        if (promptRoot != null && promptRoot.activeSelf != visible)
            promptRoot.SetActive(visible);

        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = visible ? 1f : 0f;
    }

    private void TransferPlayer(Transform player)
    {
        PlayerTransfer.MovePlayer(player, destination.arrivalPoint.position, destination.cameraBounds);
        destination.SetMinimapVisibility();
    }

    private void SetMinimapVisibility()
    {
        if (minimapRoot != null)
            minimapRoot.SetActive(!hideMinimap);
    }
}
