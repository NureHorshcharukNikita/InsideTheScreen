using System;
using UnityEngine;
using UnityEngine.Tilemaps;

// Plays a footstep from the walk animations' PlayFootstep events. Works on the player and on
// characters walked by code (a following companion), which take their sounds from the player.
public sealed class FootstepAudio : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");

    // NPCs (a following companion included) step half as loud as the player.
    private const float NpcVolumeScale = 0.5f;

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

    private bool IsWalking => movement != null
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
        if (!IsWalking)
            return;

        FootstepSurface surface = DetectSurface();
        SurfaceAudio surfaceAudio = GetSurfaceAudio(surface) ?? GetSurfaceAudio(FootstepSurface.Grass);
        if (surfaceAudio == null)
            return;

        AudioClip clip = surfaceAudio.GetNextClip();
        if (clip == null)
            return;

        float volume = surfaceAudio.VolumeScale * (movement != null ? 1f : NpcVolumeScale);
        AudioManager.TryPlaySfx(clip, volume);
    }

    private FootstepSurface DetectSurface()
    {
        Vector3 samplePosition = transform.position + (Vector3)sampleOffset;

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
