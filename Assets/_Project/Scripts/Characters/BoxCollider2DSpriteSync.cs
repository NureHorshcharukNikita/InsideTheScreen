using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BoxCollider2DSpriteSync : MonoBehaviour
{
    [SerializeField, Tooltip("Collider to resize. If empty, uses the first BoxCollider2D on this object.")]
    private BoxCollider2D targetCollider;

    [Header("Manual Size")]
    [SerializeField] private bool useManualSize;
    [SerializeField] private bool syncWidthToSprite;
    [SerializeField] private bool syncHeightToSprite;

    private void OnEnable()
    {
        Sync();
    }

    private void OnValidate()
    {
        Sync();
    }

    private void Sync()
    {
        if (useManualSize && !syncWidthToSprite && !syncHeightToSprite)
            return;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        BoxCollider2D box = TargetCollider;
        if (sr == null || sr.sprite == null || box == null)
            return;

        Bounds lb = sr.localBounds;
        if (!useManualSize)
        {
            box.offset = lb.center;
            box.size = lb.size;
            return;
        }

        Vector2 size = box.size;
        Vector2 offset = box.offset;
        if (syncWidthToSprite)
        {
            size.x = lb.size.x;
            offset.x = lb.center.x;
        }

        if (syncHeightToSprite)
        {
            size.y = lb.size.y;
            offset.y = lb.center.y;
        }

        box.offset = offset;
        box.size = size;
    }

    private BoxCollider2D TargetCollider => targetCollider != null ? targetCollider : GetComponent<BoxCollider2D>();

    public void RefreshFromSprite() => Sync();

    public void ApplyManualBounds(Vector2 offset, Vector2 size)
    {
        useManualSize = true;
        syncWidthToSprite = false;
        syncHeightToSprite = false;

        BoxCollider2D box = TargetCollider;
        if (box == null)
            return;

        box.offset = offset;
        box.size = new Vector2(Mathf.Max(0f, size.x), Mathf.Max(0f, size.y));
    }
}
