using System;
using UnityEngine;
using UnityEngine.Tilemaps;

// Plays a footstep from the walk animations' PlayFootstep events. Works on the player and on
// characters walked by code (a following companion), which take their sounds from the player. In
// among trees one walks through (Thicket), the step rustles the leaves too. Steps far off are heard
// quieter, and not at all past what the minimap shows (HeardAt).
public sealed class FootstepAudio : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");

    // NPCs (a following companion included) step half as loud as the player.
    private const float NpcVolumeScale = 0.5f;

    // How far off a step is heard (HeardAt): in full in the camera's view, a little quieter just past
    // its edge, then fading out to nothing at the edge of what the minimap shows, e.g. Brannoc walking
    // off down the south road at night.
    private const float EdgeOfViewVolume = 0.6f;
    // Still heard in full this far past the edge of the view, in units: someone stepping out of it.
    private const float ViewMargin = 0.5f;
    // No minimap camera on (in a house): heard up to this many times the view's size.
    private const float ReachWithoutMinimap = 3f;

    private static Camera minimapCamera;

    [Serializable]
    private sealed class SurfaceAudio
    {
        [SerializeField] private FootstepSurface surface = FootstepSurface.Grass;
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volumeScale = 0.8f;

        private int nextClipIndex;

        public FootstepSurface Surface => surface;
        public float VolumeScale => volumeScale;

        public SurfaceAudio()
        {
        }

        public SurfaceAudio(FootstepSurface surface)
        {
            this.surface = surface;
        }

        public AudioClip GetNextClip()
        {
            if (clips == null || clips.Length == 0)
                return null;

            AudioClip clip = clips[nextClipIndex];
            nextClipIndex = (nextClipIndex + 1) % clips.Length;
            return clip;
        }
    }

    [Header("Sampling")]
    [SerializeField] private Vector2 sampleOffset = new(0f, -0.35f);

    [Header("Surfaces")]
    [HideInInspector]
    [SerializeField] private Tilemap[] grassTilemaps;
    [HideInInspector]
    [SerializeField] private Tilemap[] pathTilemaps;
    [HideInInspector]
    [SerializeField] private Tilemap[] woodTilemaps;
    [SerializeField] private SurfaceAudio[] surfaces = CreateDefaultSurfaces();
    [Tooltip("Use the player's footstep sounds instead of the ones above (e.g. for a companion).")]
    [SerializeField] private bool sameSoundsAsPlayer;

    private PlayerMovement movement;
    private Animator animator;
    // One step a frame. Story events run the animator on unscaled time, so the first frame back
    // from a minimized window makes up for all the time away and fires every step of it at once.
    private int lastStepFrame = -1;

    // Set by a story event walking the character unheard, e.g. outside, out of view (WalkToStep).
    public bool Muted { get; set; }

    // A story event walking the character counts too: the game is not in Gameplay then, and the
    // walk animation is all there is to go by.
    private bool IsWalking => StoryEventContext.IsControlledByStory(transform)
        ? animator != null && animator.GetBool(IsMovingParameter)
        : movement != null
            ? movement.IsMoving
            : GameStateManager.IsGameplay && animator != null && animator.GetBool(IsMovingParameter);

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
        AutoFillTilemaps();
    }

    private void Start()
    {
        if (!sameSoundsAsPlayer)
            return;

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null && player.TryGetComponent(out FootstepAudio playerFootsteps) && playerFootsteps != this)
            surfaces = playerFootsteps.surfaces;
    }

    private void Reset()
    {
        surfaces = CreateDefaultSurfaces();
    }

    private void OnValidate()
    {
        if (surfaces == null || surfaces.Length == 0)
            surfaces = CreateDefaultSurfaces();
    }

    public void PlayFootstep()
    {
        if (Muted || !IsWalking || Time.frameCount == lastStepFrame)
            return;

        lastStepFrame = Time.frameCount;
        PlayStep(transform, movement != null ? 1f : NpcVolumeScale);
    }

    // A step of a character without walk animations or footsteps of their own, e.g. Garr walked by
    // a story event: the player's sound for the ground under them, as loud as an NPC's.
    public static void PlayStepFor(Transform walker)
    {
        PlayerMovement player = walker != null ? FindFirstObjectByType<PlayerMovement>() : null;
        if (player != null && player.TryGetComponent(out FootstepAudio playerFootsteps))
            playerFootsteps.PlayStep(walker, NpcVolumeScale);
    }

    private void PlayStep(Transform walker, float volumeScale)
    {
        float heard = HeardAt(walker.position);
        if (heard <= 0f)
            return;

        volumeScale *= heard;
        Thicket.Rustle(Thicket.FeetOf(walker), volumeScale);

        FootstepSurface surface = DetectSurface(walker.position);
        SurfaceAudio surfaceAudio = GetSurfaceAudio(surface) ?? GetSurfaceAudio(FootstepSurface.Grass);
        if (surfaceAudio == null)
            return;

        AudioClip clip = surfaceAudio.GetNextClip();
        if (clip == null)
            return;

        AudioManager.TryPlaySfx(clip, surfaceAudio.VolumeScale * volumeScale);
    }

    // How loud a step there is heard, 0..1: 1 in the camera's view, Edge Of View Volume just past it,
    // fading to 0 at the edge of what the minimap camera films (both around where the camera looks,
    // so a cutscene looking at someone far off hears them). Out past that it is not heard at all.
    public static float HeardAt(Vector3 position)
    {
        Camera view = Camera.main;
        if (view == null || !view.orthographic)
            return 1f;

        Vector2 offset = position - view.transform.position;
        float x = Mathf.Abs(offset.x);
        float y = Mathf.Abs(offset.y);
        Vector2 inner = new(view.orthographicSize * view.aspect + ViewMargin, view.orthographicSize + ViewMargin);
        if (x <= inner.x && y <= inner.y)
            return 1f;

        Vector2 outer = MinimapReach(inner);
        float beyond = Mathf.Max(Beyond(x, inner.x, outer.x), Beyond(y, inner.y, outer.y));
        return EdgeOfViewVolume * (1f - beyond);
    }

    private static float Beyond(float distance, float inner, float outer)
    {
        return Mathf.Clamp01((distance - inner) / Mathf.Max(0.01f, outer - inner));
    }

    // Half the size of what the minimap shows, never inside the view.
    private static Vector2 MinimapReach(Vector2 view)
    {
        Camera minimap = FindMinimapCamera();
        if (minimap == null)
            return view * ReachWithoutMinimap;

        RenderTexture texture = minimap.targetTexture;
        float aspect = texture != null && texture.height > 0 ? (float)texture.width / texture.height : minimap.aspect;
        float half = minimap.orthographicSize;
        return new Vector2(Mathf.Max(half * aspect, view.x + 1f), Mathf.Max(half, view.y + 1f));
    }

    // The camera filming the world into the minimap's texture; none while it is off (in a house).
    private static Camera FindMinimapCamera()
    {
        if (minimapCamera != null && minimapCamera.isActiveAndEnabled)
            return minimapCamera;

        minimapCamera = null;
        Camera main = Camera.main;
        foreach (Camera camera in Camera.allCameras)
        {
            if (camera != main && camera.orthographic && camera.targetTexture != null)
            {
                minimapCamera = camera;
                break;
            }
        }

        return minimapCamera;
    }

    private FootstepSurface DetectSurface(Vector3 position)
    {
        Vector3 samplePosition = position + (Vector3)sampleOffset;

        if (HasTileAt(woodTilemaps, samplePosition))
            return FootstepSurface.Wood;

        if (HasTileAt(pathTilemaps, samplePosition))
            return FootstepSurface.Path;

        if (HasTileAt(grassTilemaps, samplePosition))
            return FootstepSurface.Grass;

        return FootstepSurface.Grass;
    }

    private static bool HasTileAt(Tilemap[] tilemaps, Vector3 worldPosition)
    {
        if (tilemaps == null)
            return false;

        foreach (Tilemap tilemap in tilemaps)
        {
            if (tilemap == null)
                continue;

            Vector3Int cell = tilemap.WorldToCell(worldPosition);
            if (tilemap.HasTile(cell))
                return true;
        }

        return false;
    }

    private SurfaceAudio GetSurfaceAudio(FootstepSurface surface)
    {
        if (surfaces == null)
            return null;

        foreach (SurfaceAudio surfaceAudio in surfaces)
        {
            if (surfaceAudio != null && surfaceAudio.Surface == surface)
                return surfaceAudio;
        }

        return null;
    }

    private void AutoFillTilemaps()
    {
        Tilemap[] allTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);

        if (grassTilemaps == null || grassTilemaps.Length == 0)
            grassTilemaps = FindTilemapsByName(allTilemaps, "grass");

        if (pathTilemaps == null || pathTilemaps.Length == 0)
            pathTilemaps = FindTilemapsByName(allTilemaps, "path");

        if (woodTilemaps == null || woodTilemaps.Length == 0)
            woodTilemaps = FindTilemapsByName(allTilemaps, "bridge", "floor");
    }

    private static Tilemap[] FindTilemapsByName(Tilemap[] tilemaps, params string[] nameParts)
    {
        if (tilemaps == null || nameParts == null || nameParts.Length == 0)
            return Array.Empty<Tilemap>();

        return Array.FindAll(tilemaps, tilemap =>
            tilemap != null &&
            Array.Exists(nameParts, namePart =>
                !string.IsNullOrWhiteSpace(namePart) &&
                tilemap.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private static SurfaceAudio[] CreateDefaultSurfaces()
    {
        return new[]
        {
            new SurfaceAudio(FootstepSurface.Grass),
            new SurfaceAudio(FootstepSurface.Path),
            new SurfaceAudio(FootstepSurface.Wood)
        };
    }
}
