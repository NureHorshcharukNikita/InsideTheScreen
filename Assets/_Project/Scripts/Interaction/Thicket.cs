using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Trees one walks through, like the forest's, whose tilemap has no collider. In among them one walks
// slower - Haru, a companion following him, an NPC walking off on his own (Background Walk) and an
// enemy chasing him alike - and every step there rustles the leaves quietly over the footstep
// (Footstep Audio), like the wolf in the bushes. In among them means the walker's feet, the middle of
// their solid collider, inside a tree: the ellipse filling its sprite, a little smaller (Coverage).
// Story events walk characters at the speed they were given; their steps rustle all the same.
[RequireComponent(typeof(Tilemap))]
public sealed class Thicket : MonoBehaviour
{
    private static readonly List<Thicket> Active = new();
    private static readonly List<Collider2D> Parts = new();

    [Tooltip("How fast one walks in among the trees: 1 = as anywhere, 0.5 = half as fast.")]
    [SerializeField, Range(0.2f, 1f)] private float speedFactor = 0.6f;
    [Tooltip("How much of a tree's sprite counts as in among it: 1 = the whole ellipse filling the sprite, less = only nearer its middle.")]
    [SerializeField, Range(0.3f, 1f)] private float coverage = 0.85f;
    [Tooltip("A rustle for a step in among the trees, a different one each step. Empty = steps there sound as anywhere.")]
    [SerializeField] private AudioClip[] rustles; // [SFX] sfx_thicket_rustle
    [Tooltip("How loud a rustle is for Haru. NPCs' are half as loud, like their footsteps.")]
    [SerializeField, Range(0f, 1f)] private float rustleVolume = 0.3f;

    private Tilemap tilemap;
    // How far the trees' sprites reach from the point they are drawn at, in world units.
    private Bounds reach;
    private int lastRustle = -1;

    // How fast one walks with these feet: Speed Factor in among the trees, 1 anywhere else.
    public static float SpeedAt(Vector2 feet)
    {
        float factor = 1f;
        foreach (Thicket thicket in Active)
        {
            if (thicket.speedFactor < factor && thicket.Contains(feet))
                factor = thicket.speedFactor;
        }

        return factor;
    }

    // A step with these feet: in among the trees, a rustle too.
    public static void Rustle(Vector2 feet, float volumeScale)
    {
        foreach (Thicket thicket in Active)
        {
            if (thicket.Contains(feet))
            {
                thicket.PlayRustle(volumeScale);
                return;
            }
        }
    }

    // Where a character stands: the middle of their solid collider, as Companion Follower and Enemy
    // Chaser go by; without one, where they are.
    public static Vector2 FeetOf(Component walker)
    {
        walker.GetComponents(Parts);
        foreach (Collider2D part in Parts)
        {
            if (part.enabled && !part.isTrigger)
                return part.bounds.center;
        }

        return walker.transform.position;
    }

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        reach = MeasureReach();
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private bool Contains(Vector2 point)
    {
        if (reach.size == Vector3.zero)
            return false;

        // A tree drawn from a cell below reaches up over the point: the cells whose trees could.
        Vector3Int from = tilemap.WorldToCell(point - (Vector2)reach.max);
        Vector3Int to = tilemap.WorldToCell(point - (Vector2)reach.min);
        for (int y = from.y; y <= to.y; y++)
        {
            for (int x = from.x; x <= to.x; x++)
            {
                Vector3Int cell = new(x, y, from.z);
                Sprite sprite = tilemap.GetSprite(cell);
                if (sprite == null)
                    continue;

                Vector2 offset = point - DrawnAt(cell) - (Vector2)sprite.bounds.center;
                Vector2 radius = (Vector2)sprite.bounds.extents * coverage;
                if (radius.x <= 0f || radius.y <= 0f)
                    continue;

                float across = offset.x / radius.x;
                float up = offset.y / radius.y;
                if (across * across + up * up <= 1f)
                    return true;
            }
        }

        return false;
    }

    // Where a tile's sprite is drawn from: its pivot sits on the tile anchor of the cell.
    private Vector2 DrawnAt(Vector3Int cell)
    {
        return tilemap.LocalToWorld(tilemap.CellToLocalInterpolated((Vector3)cell + tilemap.tileAnchor));
    }

    private Bounds MeasureReach()
    {
        TileBase[] used = new TileBase[tilemap.GetUsedTilesCount()];
        tilemap.GetUsedTilesNonAlloc(used);
        Bounds bounds = default;
        bool any = false;
        foreach (TileBase tileBase in used)
        {
            if (tileBase is not Tile tile || tile.sprite == null)
                continue;

            if (any)
                bounds.Encapsulate(tile.sprite.bounds);
            else
                bounds = tile.sprite.bounds;
            any = true;
        }

        return bounds;
    }

    private void PlayRustle(float volumeScale)
    {
        if (rustles == null || rustles.Length == 0)
            return;

        int index = Random.Range(0, rustles.Length);
        if (rustles.Length > 1 && index == lastRustle)
            index = (index + 1) % rustles.Length;

        lastRustle = index;
        if (rustles[index] != null)
            AudioManager.TryPlaySfx(rustles[index], rustleVolume * volumeScale);
    }
}
