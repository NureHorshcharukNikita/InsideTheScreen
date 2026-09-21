using TMPro;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class HouseDoorway : MonoBehaviour
{
    [SerializeField] private HouseDoorway destination;
    [SerializeField] private Transform arrivalPoint;
    [SerializeField] private Collider2D cameraBounds;
    [SerializeField] private CinemachineConfiner2D mainCameraConfiner;
    [SerializeField] private CinemachineConfiner2D minimapCameraConfiner;
    [SerializeField] private GameObject minimapRoot;
    [SerializeField] private bool hideMinimap;
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private string promptAction = "Enter";

    private static bool transitionInProgress;
    private PlayerMovement nearbyPlayer;

    private void Awake()
    {
        if (promptRoot == null || promptCanvasGroup == null || promptText == null)
            Debug.LogWarning($"House doorway {name} has incomplete prompt references.", this);

        SetPromptVisible(false);
    }

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Start()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null && cameraBounds != null && cameraBounds.OverlapPoint(player.transform.position))
        {
            SetCameraBounds(cameraBounds);
            SetMinimapVisibility();
        }
    }

    private void Update()
    {
        bool showPrompt = nearbyPlayer != null && GameStateManager.IsGameplay && !transitionInProgress;
        if (showPrompt && promptRoot != null && !promptRoot.activeSelf)
            RefreshPrompt();
        SetPromptVisible(showPrompt);

        if (showPrompt && GameInputSettings.GetKeyDown(GameInputAction.Interact))
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
        if (promptText != null)
            promptText.text = $"{GameInputSettings.GetDisplayName(GameInputAction.Interact)} - {promptAction}";
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null && promptRoot.activeSelf != visible)
            promptRoot.SetActive(visible);

        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = visible ? 1f : 0f;
    }

    private void TransferPlayer(Transform player)
    {
        Vector3 oldPosition = player.position;
        Vector3 newPosition = destination.arrivalPoint.position;
        newPosition.z = oldPosition.z;

        if (player.TryGetComponent(out Rigidbody2D body))
        {
            body.linearVelocity = Vector2.zero;
            body.position = newPosition;
        }

        player.position = newPosition;
        Physics2D.SyncTransforms();
        SetCameraBounds(destination.cameraBounds);
        destination.SetMinimapVisibility();

        CinemachineCamera camera = mainCameraConfiner != null
            ? mainCameraConfiner.GetComponent<CinemachineCamera>()
            : null;
        if (camera != null)
        {
            camera.OnTargetObjectWarped(player, newPosition - oldPosition);
            camera.PreviousStateIsValid = false;
        }

        ExplorationPlayerSession.SavePosition(newPosition);
    }

    private void SetMinimapVisibility()
    {
        if (minimapRoot != null)
            minimapRoot.SetActive(!hideMinimap);
    }

    private void SetCameraBounds(Collider2D bounds)
    {
        SetConfinerBounds(mainCameraConfiner, bounds);
        SetConfinerBounds(minimapCameraConfiner, bounds);
    }

    private static void SetConfinerBounds(CinemachineConfiner2D confiner, Collider2D bounds)
    {
        if (confiner == null || bounds == null || confiner.BoundingShape2D == bounds)
            return;

        confiner.BoundingShape2D = bounds;
        confiner.InvalidateBoundingShapeCache();
        CinemachineCamera camera = confiner.GetComponent<CinemachineCamera>();
        if (camera != null)
            camera.PreviousStateIsValid = false;
    }
}
