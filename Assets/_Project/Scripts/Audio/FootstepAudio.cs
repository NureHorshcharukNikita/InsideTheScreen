using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(PlayerMovement))]
public sealed class FootstepAudio : MonoBehaviour
{
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
    [SerializeField] private SurfaceAudio[] surfaces = CreateDefaultSurfaces();

    private PlayerMovement movement;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        AutoFillTilemaps();
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
        if (movement == null || !movement.IsMoving)
            return;

        FootstepSurface surface = DetectSurface();
        SurfaceAudio surfaceAudio = GetSurfaceAudio(surface) ?? GetSurfaceAudio(FootstepSurface.Grass);
        if (surfaceAudio == null)
            return;

        AudioClip clip = surfaceAudio.GetNextClip();
        if (clip == null)
            return;

        AudioManager.TryPlaySfx(clip, surfaceAudio.VolumeScale);
    }

    private FootstepSurface DetectSurface()
    {
        Vector3 samplePosition = transform.position + (Vector3)sampleOffset;

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
    }

    private static Tilemap[] FindTilemapsByName(Tilemap[] tilemaps, string namePart)
    {
        if (tilemaps == null || string.IsNullOrWhiteSpace(namePart))
            return Array.Empty<Tilemap>();

        return Array.FindAll(tilemaps, tilemap =>
            tilemap != null &&
            tilemap.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static SurfaceAudio[] CreateDefaultSurfaces()
    {
        return new[]
        {
            new SurfaceAudio(FootstepSurface.Grass),
            new SurfaceAudio(FootstepSurface.Path)
        };
    }
}
