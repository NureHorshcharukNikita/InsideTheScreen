using UnityEngine;

// What a character holds in a story scene: a card taken out (its mini card, Card Turn step), or the
// thing it turned into, e.g. the stick Haru's card turns back into, swung when he strikes (Strike
// step). Drawn at the hand on one side, over the character, it stays there as they move until it is
// put away (or the story event is over).
public sealed class HeldItem : MonoBehaviour
{
    // Where the hand is, as a share of the character's sprite: across from its middle, up from its bottom.
    private static readonly Vector2 HandAt = new(0.3f, 0.42f);
    // A card's middle from the hand, in units: in the hand, a little up, held out.
    private static readonly Vector2 CardOverHand = new(0.05f, 0.2f);
    // Held out towards the one he shows it to: its top leans that way, in degrees.
    private const float CardTilt = 20f;
    // A card in the hand is smaller than a mini card over the head (Pickup Card): about a hand's size.
    public const float CardSize = 0.6f;
    // Held in the hand, its tip diagonally down and forward, towards the one in front, on either side: a
    // thing drawn leaning "/" (a stick, a blade) turned a little over a quarter round.
    public const float RestAngle = -100f;

    private SpriteRenderer body;
    private SpriteRenderer drawn;
    private GameObject card;

    // The hand's side: 1 right, -1 left.
    public float Side { get; set; } = 1f;
    public float Angle { get; set; } = RestAngle;
    // Across, 0 to 1: squeezed flat as it turns into a card or out of one.
    public float Width { get; set; } = 1f;
    public GameObject Card => card;

    public static HeldItem Of(Transform character)
    {
        return character != null ? character.GetComponent<HeldItem>() : null;
    }

    // The thing itself in the hand, in place of a card held.
    public static HeldItem Take(Transform character, Sprite sprite, float side)
    {
        if (character == null || sprite == null)
            return null;

        HeldItem held = Get(character);
        held.DropCard();
        held.Show(sprite, side);
        return held;
    }

    // A card held up in the hand (made by the Card Turn step); it goes with the held thing.
    public static HeldItem TakeCard(Transform character, GameObject shownCard, float side)
    {
        if (character == null || shownCard == null)
            return null;

        HeldItem held = Get(character);
        held.DropItem();
        held.DropCard();
        held.card = shownCard;
        held.body = held.FindBody();
        held.Side = side;
        held.Width = 1f;
        held.LateUpdate();
        return held;
    }

    public static void PutAway(Transform character)
    {
        HeldItem held = Of(character);
        if (held != null)
            Destroy(held);
    }

    // Where the hand is now on that side, in the world.
    public static Vector3 HandOf(SpriteRenderer body, float side)
    {
        Bounds bounds = body.bounds;
        return new Vector3(bounds.center.x + side * HandAt.x * bounds.size.x, bounds.min.y + HandAt.y * bounds.size.y,
            body.transform.position.z);
    }

    // Where a card held in that hand has its middle.
    public static Vector3 CardAt(SpriteRenderer body, float side)
    {
        return HandOf(body, side) + new Vector3(side * CardOverHand.x, CardOverHand.y, 0f);
    }

    // A card held out that way: leaning towards the one it is shown to.
    public static Quaternion CardTiltTo(float side)
    {
        return Quaternion.Euler(0f, 0f, -side * CardTilt);
    }

    // Where a card goes when it is put away: the coat pocket, at the middle of the character.
    public static Vector3 PocketOf(SpriteRenderer body)
    {
        Bounds bounds = body.bounds;
        return new Vector3(bounds.center.x, bounds.min.y + 0.38f * bounds.size.y, body.transform.position.z);
    }

    // The card held is let go of (to be turned or put away by whoever took it).
    public GameObject ReleaseCard()
    {
        GameObject released = card;
        card = null;
        return released;
    }

    private static HeldItem Get(Transform character)
    {
        if (!character.TryGetComponent(out HeldItem held))
            held = character.gameObject.AddComponent<HeldItem>();

        return held;
    }

    private void Show(Sprite sprite, float side)
    {
        if (drawn == null)
            drawn = new GameObject("HeldItem").AddComponent<SpriteRenderer>();

        body = FindBody();
        drawn.sprite = sprite;
        if (body != null)
        {
            drawn.sortingLayerID = body.sortingLayerID;
            drawn.sortingOrder = body.sortingOrder + 1;
            drawn.sharedMaterial = body.sharedMaterial;
        }

        Side = side;
        Angle = RestAngle;
        Width = 1f;
        LateUpdate();
    }

    // The character's own sprite, not the held thing's.
    private SpriteRenderer FindBody()
    {
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>())
        {
            if (sprite != drawn)
                return sprite;
        }

        return null;
    }

    private void LateUpdate()
    {
        if (body == null)
            return;

        if (drawn != null)
        {
            drawn.transform.position = HandOf(body, Side);
            drawn.transform.localScale = new Vector3(Side * Width, 1f, 1f);
            drawn.transform.rotation = Quaternion.Euler(0f, 0f, Side * Angle);
        }

        if (card != null)
        {
            card.transform.position = CardAt(body, Side);
            card.transform.rotation = CardTiltTo(Side);
            card.transform.localScale = new Vector3(Width * CardSize, CardSize, 1f);
        }
    }

    private void DropItem()
    {
        if (drawn != null)
            Destroy(drawn.gameObject);
        drawn = null;
    }

    private void DropCard()
    {
        if (card != null)
            Destroy(card);
        card = null;
    }

    private void OnDestroy()
    {
        DropItem();
        DropCard();
    }
}
