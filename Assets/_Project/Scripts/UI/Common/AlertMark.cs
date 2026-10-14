using UnityEngine;

// The "!" over a character who notices something: an enemy spotting the player (EnemyChaser), a
// character in a story event (ExclaimStep). The ExclamationMark sprite, as a child of the
// character just above its sprite, drawn over it. Kept in Resources so both find the sprite
// without each holding a reference.
[CreateAssetMenu(menuName = "Game/UI/Alert Mark", fileName = ResourceName)]
public sealed class AlertMark : ScriptableObject
{
    public const string ResourceName = "AlertMark";

    // Between the top of the head and the bottom of the "!": about 5 pixels on screen.
    private const float Gap = 0.05f;

    [SerializeField] private Sprite sprite;
    [Tooltip("Height of the \"!\" in world units, whatever the character's scale.")]
    [SerializeField, Min(0.1f)] private float height = 1f;

    private static AlertMark instance;

    private static AlertMark Instance =>
        instance != null ? instance : instance = Resources.Load<AlertMark>(ResourceName);

    public static GameObject Create(Transform character)
    {
        var markObject = new GameObject("AlertMark");
        Transform markTransform = markObject.transform;
        markTransform.SetParent(character, false);

        AlertMark settings = Instance;
        if (settings == null || settings.sprite == null)
        {
            Debug.LogWarning($"No \"!\" sprite: assign it on Resources/{ResourceName}.", settings);
            return markObject;
        }

        SpriteRenderer characterSprite = character.GetComponentInChildren<SpriteRenderer>();
        SpriteRenderer mark = markObject.AddComponent<SpriteRenderer>();
        mark.sprite = settings.sprite;
        if (characterSprite != null)
        {
            mark.sortingLayerID = characterSprite.sortingLayerID;
            mark.sortingOrder = characterSprite.sortingOrder + 1;
        }

        // The same size on screen whatever the character's scale, and never mirrored with it.
        Bounds shape = settings.sprite.bounds;
        float size = settings.height / shape.size.y;
        Vector3 characterScale = character.lossyScale;
        markTransform.localScale = new Vector3(size / NonZero(characterScale.x), size / NonZero(characterScale.y), 1f);

        // Centred over the character, its bottom Gap above the head, wherever the sprite's pivot is.
        float top = characterSprite != null ? characterSprite.bounds.max.y : character.position.y;
        markTransform.position = new Vector3(
            character.position.x - shape.center.x * size,
            top + Gap - shape.min.y * size,
            character.position.z);

        return markObject;
    }

    private static float NonZero(float value)
    {
        return Mathf.Approximately(value, 0f) ? 1f : value;
    }
}
