using UnityEngine;

// Puts a character's map look (CharacterProfile.MapSprite) on its SpriteRenderer, so an enemy or an
// NPC on the map needs only its data; the collider follows the sprite (BoxCollider2DSpriteSync).
public static class MapSprite
{
    public static void Apply(GameObject target, Sprite sprite)
    {
        if (sprite == null || target == null || !target.TryGetComponent(out SpriteRenderer spriteRenderer)
            || spriteRenderer.sprite == sprite)
            return;

        spriteRenderer.sprite = sprite;
        if (target.TryGetComponent(out BoxCollider2DSpriteSync colliderSync))
            colliderSync.RefreshFromSprite();
    }
}
