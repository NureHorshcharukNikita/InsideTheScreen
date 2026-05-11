using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BoxCollider2DSpriteSync : MonoBehaviour
{
    private void Awake()
    {
        Sync();
    }

    private void OnValidate()
    {
        Sync();
    }

    private void Sync()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (box == null || sr == null || sr.sprite == null)
            return;

        Bounds lb = sr.localBounds;
        box.offset = (Vector2)lb.center;
        box.size = (Vector2)lb.size;
    }
}
