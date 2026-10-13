using TMPro;
using UnityEngine;

// The yellow "!" over a character who notices something: an enemy spotting the player
// (EnemyChaser), a character in a story event (ExclaimStep). A child of the character, just above
// its sprite, drawn over it.
public static class AlertMark
{
    // About 1.5 world units tall, whatever the character's scale.
    private const float FontSize = 15f;
    // Between the top of the head and the bottom of the "!": about 5 pixels on screen.
    private const float Gap = 0.05f;

    public static TMP_Text Create(Transform character)
    {
        SpriteRenderer sprite = character.GetComponentInChildren<SpriteRenderer>();

        var markObject = new GameObject("AlertMark", typeof(RectTransform));
        var rect = (RectTransform)markObject.transform;
        rect.SetParent(character, false);
        // Pivot at the bottom: the mark grows upwards from just above the head. Centred, the text
        // box (5 units tall) would put the bottom-aligned "!" inside the character.
        rect.pivot = new Vector2(0.5f, 0f);

        float top = sprite != null ? sprite.bounds.max.y : character.position.y;
        rect.position = new Vector3(character.position.x, top + Gap, character.position.z);

        TextMeshPro mark = markObject.AddComponent<TextMeshPro>();
        mark.text = "!";
        mark.fontSize = FontSize / Mathf.Max(0.01f, Mathf.Abs(character.lossyScale.y));
        mark.fontStyle = FontStyles.Bold;
        mark.alignment = TextAlignmentOptions.Bottom;
        mark.textWrappingMode = TextWrappingModes.NoWrap;
        mark.color = new Color(1f, 0.85f, 0.2f);
        mark.outlineWidth = 0.25f;
        mark.outlineColor = Color.black;

        if (sprite != null)
        {
            mark.sortingLayerID = sprite.sortingLayerID;
            mark.sortingOrder = sprite.sortingOrder + 1;
        }

        // The font leaves room under the line for letters like "g": lower the mark by that much,
        // so the drawn "!" itself starts Gap above the head.
        mark.ForceMeshUpdate();
        rect.localPosition -= new Vector3(0f, mark.textBounds.min.y, 0f);

        return mark;
    }
}
