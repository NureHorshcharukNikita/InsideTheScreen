using UnityEngine;

// For a character drawn as one sprite instead of walk animations (e.g. the wolf in the forest
// story event): story steps that walk or turn it flip the sprite to face left or right.
public sealed class FlipSpriteToFace : MonoBehaviour
{
    [Tooltip("Which way the sprite looks as drawn.")]
    [SerializeField] private bool spriteFacesLeft = true;

    public static void Apply(Transform actor, Vector2 direction)
    {
        if (actor != null && actor.TryGetComponent(out FlipSpriteToFace facing))
            facing.Face(direction);
    }

    public void Face(Vector2 direction)
    {
        // Straight up or down keeps the side it faced.
        if (Mathf.Abs(direction.x) < 0.01f)
            return;

        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
            sprite.flipX = spriteFacesLeft ? direction.x > 0f : direction.x < 0f;
    }
}
