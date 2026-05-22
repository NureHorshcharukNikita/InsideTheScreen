using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))][RequireComponent(typeof(SpriteRenderer))]
public class BoxCollider2DSpriteSync : MonoBehaviour
{
    [Header("Manual Size")]
    [SerializeField] private bool useManualSize;
    [SerializeField] private bool syncWidthToSprite;
    [SerializeField] private bool syncHeightToSprite;

    private void Awake()
    {
        Sync();
    }

    private void OnEnable()
    {
        Sync();
    }

    private void Start()
    {
        Sync();
    }

    private void OnValidate()
    {
        Sync();
    }

    private void Sync()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return;

        Bounds lb = sr.localBounds;

        foreach (BoxCollider2D box in GetComponents<BoxCollider2D>())
        {
            Vector2 size = useManualSize ? box.size : (Vector2)lb.size;

            if (useManualSize)
            {
                if (syncWidthToSprite)
                    size.x = lb.size.x;

                if (syncHeightToSprite)
                    size.y = lb.size.y;
            }

            size.x = Mathf.Max(0f, size.x);
            size.y = Mathf.Max(0f, size.y);

            Vector2 center = useManualSize ? (Vector2)lb.min + size * 0.5f : (Vector2)lb.center;

            box.offset = center;
            box.size = size;
        }
    }

    public void RefreshFromSprite() => Sync();
}
