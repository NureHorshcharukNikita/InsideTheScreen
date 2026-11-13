using System.Collections;
using TMPro;
using UnityEngine;

// A mini card over Haru's head for something he picks up or finds (ForagePickup, LootChest): the
// card's frame with its icon, and how many ("x1", "x2") small on the card under the icon. It pops
// up just over his head, rises and fades, following him as he walks. Measured from the top of his
// sprite, like the "!" (AlertMark), and not his child: his scale would size and mirror it. On the
// GameClock, so it still plays out under a story event the find starts. Kept in Resources, like
// the "!", so every pickup finds the frame without holding a reference.
[CreateAssetMenu(menuName = "Game/UI/Pickup Card", fileName = ResourceName)]
public sealed class PickupCard : ScriptableObject
{
    public const string ResourceName = "PickupCard";

    private const float Lifetime = 1.3f;
    private const float PopSeconds = 0.18f;
    private const float Rise = 0.6f;
    // From the top of Haru's sprite to the bottom of the card.
    private const float Gap = 0.15f;
    // Over Haru's sprite, in this order: the frame, the icon, the count.
    private const int SortingAbove = 100;
    // How far over the card's middle the icon sits, as a share of its height: room for the count under it.
    private const float IconRaise = 0.08f;

    [Tooltip("The card's frame, as on the cards in the inventory.")]
    [SerializeField] private Sprite frame;
    [Tooltip("Height of the mini card in world units, whatever Haru's scale.")]
    [SerializeField, Min(0.2f)] private float height = 0.9f;
    [Tooltip("Width of the icon, as a share of the card's width.")]
    [SerializeField, Range(0.2f, 1f)] private float iconWidth = 0.62f;
    [Tooltip("Size of the count on the card, under the icon.")]
    [SerializeField, Min(0.5f)] private float countFontSize = 1.5f;

    private static PickupCard instance;

    private static PickupCard Instance =>
        instance != null ? instance : instance = Resources.Load<PickupCard>(ResourceName);

    // The card on its own, without the count, e.g. in Haru's hand as it turns into what it is (Card
    // Turn step). Drawn over that sprite. Null without the frame.
    public static GameObject BuildCard(CardData card, SpriteRenderer over)
    {
        PickupCard look = Instance;
        return look != null && look.frame != null && card != null ? look.Build(card, 0, over) : null;
    }

    // Delay: after how long it shows; sideways: how far left or right of Haru's middle, in units,
    // e.g. several cards out of a chest dealt side by side, one after the other.
    public static void Show(PlayerCharacter player, CardData card, int count, float delay = 0f, float sideways = 0f)
    {
        PickupCard look = Instance;
        if (player == null || card == null)
            return;

        if (look == null || look.frame == null)
        {
            Debug.LogWarning($"No card frame: assign it on Resources/{ResourceName}.", look);
            return;
        }

        player.StartCoroutine(look.Play(player, card, count, delay, sideways));
    }

    private IEnumerator Play(PlayerCharacter player, CardData card, int count, float delay, float sideways)
    {
        if (delay > 0f)
            yield return new WaitForGameSeconds(delay);

        if (player == null)
            yield break;

        SpriteRenderer body = player.GetComponentInChildren<SpriteRenderer>();
        Vector3 feet = player.transform.position;
        float top = body != null ? body.bounds.max.y : feet.y + 1.5f;
        Vector3 offset = new(sideways, top + Gap + height * 0.5f - feet.y, 0f);

        GameObject miniCard = Build(card, count, body);
        Transform shown = miniCard.transform;
        SpriteRenderer[] sprites = miniCard.GetComponentsInChildren<SpriteRenderer>();
        TMP_Text countText = miniCard.GetComponentInChildren<TMP_Text>();

        for (float elapsed = 0f; elapsed < Lifetime && miniCard != null && player != null; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / Lifetime;
            shown.position = player.transform.position + offset + Vector3.up * (Rise * t);

            // Pops up from small with a little overshoot, then holds its size.
            float pop = Mathf.Clamp01(elapsed / PopSeconds) - 1f;
            shown.localScale = Vector3.one * Mathf.LerpUnclamped(0.5f, 1f, 1f + 2.70158f * pop * pop * pop + 1.70158f * pop * pop);

            float alpha = 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
            foreach (SpriteRenderer sprite in sprites)
                sprite.color = new Color(1f, 1f, 1f, alpha);
            if (countText != null)
                countText.alpha = alpha;

            yield return null;
        }

        if (miniCard != null)
            Destroy(miniCard);
    }

    // The frame at the set height, the icon a little above its middle, the count under the icon.
    private GameObject Build(CardData card, int count, SpriteRenderer over)
    {
        GameObject miniCard = new("PickupCard");
        int layer = over != null ? over.sortingLayerID : 0;
        int order = (over != null ? over.sortingOrder : 0) + SortingAbove;

        float frameScale = height / Mathf.Max(0.01f, frame.bounds.size.y);
        AddSprite(miniCard.transform, "Frame", frame, frameScale, Vector3.zero, layer, order);
        float width = frame.bounds.size.x * frameScale;

        float iconBottom = 0f;
        if (card.Icon != null)
        {
            float iconScale = width * iconWidth / Mathf.Max(0.01f, card.Icon.bounds.size.x);
            float iconY = height * IconRaise;
            AddSprite(miniCard.transform, "Icon", card.Icon, iconScale, new Vector3(0f, iconY, 0f), layer, order + 1);
            iconBottom = iconY + card.Icon.bounds.min.y * iconScale;
        }

        // How many, also one: small, on the card halfway between the icon and the card's bottom,
        // outlined to read over the frame. None for a card on its own (count 0).
        if (count <= 0)
            return miniCard;

        GameObject countObject = new("Count");
        countObject.transform.SetParent(miniCard.transform, false);
        countObject.transform.localPosition = new Vector3(0f, (iconBottom - height * 0.5f) * 0.5f, 0f);
        TextMeshPro countText = countObject.AddComponent<TextMeshPro>();
        countText.text = $"x{Mathf.Max(1, count)}";
        countText.fontSize = countFontSize;
        countText.fontStyle = FontStyles.Bold;
        countText.alignment = TextAlignmentOptions.Center;
        countText.outlineWidth = 0.25f;
        countText.outlineColor = new Color32(0, 0, 0, 255);
        countText.rectTransform.sizeDelta = new Vector2(width, height * 0.3f);
        countText.sortingLayerID = layer;
        countText.sortingOrder = order + 2;

        return miniCard;
    }

    private static void AddSprite(Transform parent, string name, Sprite sprite, float scale, Vector3 position,
        int layer, int order)
    {
        GameObject part = new(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = layer;
        renderer.sortingOrder = order;
    }
}
