using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))][RequireComponent(typeof(SpriteRenderer))]
public class BoxCollider2DSpriteSync : MonoBehaviour
{
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
        Vector2 center = lb.center;
        Vector2 size = lb.size;

        foreach (BoxCollider2D box in GetComponents<BoxCollider2D>())
        {
            box.offset = center;
            box.size = size;
        }
    }

    public void RefreshFromSprite() => Sync();
}

