using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// A character lays cards out of the coat pocket onto something in front of it, one after another; once
// they all lie there, they turn into what they are at once, e.g. Haru putting all the salve's herbs on
// Garr's workbench before the recipe comes to him. Pick Up: what lies there turns back into its cards,
// which go back into the pocket one by one. Whatever still lies there is gone with the event.
[Serializable, SubclassMenu("Characters/Lay Out Cards")]
public sealed class LayOutCardsStep : StoryStep
{
    public enum Way
    {
        LayOut,
        PickUp
    }

    [Serializable]
    public sealed class LaidCard
    {
        public CardData card;
        [Tooltip("What it turns into, lying there, e.g. the leaves as they lie on the ground. Empty = it stays a card.")]
        public Sprite item;
    }

    private sealed class Lying
    {
        public GameObject Root;
        public readonly List<Thing> Things = new();
    }

    private sealed class Thing
    {
        public GameObject Card;
        public SpriteRenderer Item;
        public Vector3 CardAt;
    }

    private const float FlySeconds = 0.35f;
    private const float BackSeconds = 0.28f;
    private const float FlipSeconds = 0.12f;
    private const float ArcHeight = 0.45f;
    // A card's middle above where its thing lies: it stands on the top, not in it.
    private const float CardLift = 0.32f;
    // Drawn this much nearer than what they lie on: just in front of it, behind anyone standing before it.
    private const float DepthNearer = 0.02f;

    // What each character has laid out in this event, for Pick Up.
    private static readonly Dictionary<Transform, Lying> LaidOut = new();

    [SerializeField] private StoryTarget actor = new(StoryTargetType.Player);
    [Tooltip("Lay Out: from the pocket onto the place, then they turn into what they are. Pick Up: back into cards and into the pocket.")]
    [SerializeField] private Way way = Way.LayOut;
    [Tooltip("Lay Out: the cards, left to right.")]
    [SerializeField] private List<LaidCard> cards = new();
    [Tooltip("Lay Out: the middle of where they lie, e.g. a point on the workbench's top (things stand on it).")]
    [SerializeField] private StoryTarget onto = new(StoryTargetType.SceneObject);
    [Tooltip("Lay Out: what they lie on, e.g. the workbench: drawn just in front of it, so someone standing before it still hides them. Empty = over everything, like a card held in the hand.")]
    [SerializeField] private SpriteRenderer over;
    [Tooltip("Lay Out: how far apart they lie, in units.")]
    [SerializeField, Min(0f)] private float spacing = 0.5f;
    [Tooltip("Lay Out: how big a thing is lying there, against its size on the ground.")]
    [SerializeField, Min(0.05f)] private float itemSize = 0.45f;
    [Tooltip("How long after one card the next goes.")]
    [SerializeField, Min(0f)] private float secondsApart = 0.15f;
    [Tooltip("Heard as each card lands, or as each goes back. Empty = nothing.")]
    [SerializeField] private AudioClip sound;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlayMode()
    {
        LaidOut.Clear();
    }

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform character = actor.Resolve(context);
        SpriteRenderer body = character != null ? character.GetComponentInChildren<SpriteRenderer>() : null;
        if (body == null)
            yield break;

        context.TakeControl(character);
        if (way == Way.LayOut)
            yield return LayOut(context, character, body);
        else
            yield return PickUp(character, body);
    }

    private IEnumerator LayOut(StoryEventContext context, Transform character, SpriteRenderer body)
    {
        Transform place = onto.Resolve(context);
        if (place == null || cards.Count == 0)
            yield break;

        Clear(character);
        Lying lying = new() { Root = new GameObject("LaidOutCards") };
        LaidOut[character] = lying;
        context.OnRelease(() => Clear(character));

        SpriteRenderer drawnOver = over != null ? over : body;
        if (over != null)
        {
            lying.Root.transform.position = new Vector3(place.position.x, over.transform.position.y - DepthNearer, place.position.z);
            SortingGroup group = lying.Root.AddComponent<SortingGroup>();
            group.sortingLayerID = over.sortingLayerID;
            group.sortingOrder = over.sortingOrder;
        }

        // One after another out of the pocket, in an arc, onto the place.
        for (int i = 0; i < cards.Count; i++)
        {
            LaidCard laid = cards[i];
            GameObject card = laid != null ? PickupCard.BuildCard(laid.card, drawnOver) : null;
            if (card == null)
                continue;

            card.transform.SetParent(lying.Root.transform, true);
            float across = (i - (cards.Count - 1) * 0.5f) * spacing;
            Vector3 lies = place.position + new Vector3(across, 0f, 0f);
            Thing thing = new() { Card = card, CardAt = lies + Vector3.up * CardLift };
            if (laid.item != null)
                thing.Item = MakeItem(lying.Root.transform, laid.item, lies, card);

            lying.Things.Add(thing);
            yield return Fly(card, HeldItem.PocketOf(body), thing.CardAt, FlySeconds, false);
            AudioManager.TryPlaySfx(sound, volume);
            if (secondsApart > 0f && i < cards.Count - 1)
                yield return new WaitForGameSeconds(secondsApart);
        }

        // All of them at once: the cards squeeze flat, and what they are opens out in their place.
        yield return new WaitForGameSeconds(0.3f);
        yield return Turn(lying, true);
    }

    private IEnumerator PickUp(Transform character, SpriteRenderer body)
    {
        if (!LaidOut.TryGetValue(character, out Lying lying) || lying.Root == null)
            yield break;

        yield return Turn(lying, false);
        for (int i = lying.Things.Count - 1; i >= 0; i--)
        {
            Thing thing = lying.Things[i];
            AudioManager.TryPlaySfx(sound, volume);
            yield return Fly(thing.Card, thing.CardAt, HeldItem.PocketOf(body), BackSeconds, true);
            if (thing.Card != null)
                thing.Card.SetActive(false);
            if (secondsApart > 0f && i > 0)
                yield return new WaitForGameSeconds(secondsApart * 0.5f);
        }

        Clear(character);
    }

    // Into the things (or back into cards): the one squeezes flat across, the other opens out.
    private IEnumerator Turn(Lying lying, bool intoThings)
    {
        for (float elapsed = 0f; elapsed < FlipSeconds * 2f; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / FlipSeconds;
            float closing = Mathf.Clamp01(1f - t);
            float opening = Mathf.Clamp01(t - 1f);
            foreach (Thing thing in lying.Things)
            {
                if (thing.Item == null)
                    continue;

                SetWidth(thing.Card, intoThings ? closing : opening);
                SetWidth(thing.Item, intoThings ? opening : closing);
            }

            yield return null;
        }

        foreach (Thing thing in lying.Things)
        {
            if (thing.Item == null)
                continue;

            SetWidth(thing.Card, intoThings ? 0f : 1f);
            SetWidth(thing.Item, intoThings ? 1f : 0f);
        }
    }

    // From the pocket to its place, growing and coming into view; or back, shrinking and fading.
    private static IEnumerator Fly(GameObject card, Vector3 from, Vector3 to, float seconds, bool intoPocket)
    {
        if (card == null)
            yield break;

        SpriteRenderer[] sprites = card.GetComponentsInChildren<SpriteRenderer>();
        for (float elapsed = 0f; elapsed < seconds; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / seconds;
            float eased = intoPocket ? t * t : 1f - (1f - t) * (1f - t);
            float shown = intoPocket ? 1f - t : Mathf.Clamp01(t * 3f);
            float size = intoPocket ? Mathf.Lerp(1f, 0.4f, t) : Mathf.Lerp(0.4f, 1f, eased);
            Place(card, Vector3.Lerp(from, to, eased) + Vector3.up * (ArcHeight * Mathf.Sin(t * Mathf.PI)), size, sprites, shown);
            yield return null;
        }

        Place(card, to, intoPocket ? 0.4f : 1f, sprites, intoPocket ? 0f : 1f);
    }

    private static void Place(GameObject card, Vector3 at, float size, SpriteRenderer[] sprites, float shown)
    {
        card.transform.position = at;
        card.transform.rotation = Quaternion.identity;
        card.transform.localScale = new Vector3(size, size, 1f) * HeldItem.CardSize;
        foreach (SpriteRenderer sprite in sprites)
        {
            if (sprite != null)
                sprite.color = new Color(1f, 1f, 1f, shown);
        }
    }

    private SpriteRenderer MakeItem(Transform root, Sprite item, Vector3 lies, GameObject card)
    {
        SpriteRenderer drawn = new GameObject("LaidItem").AddComponent<SpriteRenderer>();
        drawn.transform.SetParent(root, true);
        drawn.transform.position = lies;
        drawn.sprite = item;
        SpriteRenderer frame = card.GetComponentInChildren<SpriteRenderer>();
        if (frame != null)
        {
            drawn.sortingLayerID = frame.sortingLayerID;
            drawn.sortingOrder = frame.sortingOrder;
            drawn.sharedMaterial = frame.sharedMaterial;
        }

        drawn.transform.localScale = new Vector3(0f, itemSize, 1f);
        return drawn;
    }

    private void SetWidth(GameObject card, float width)
    {
        if (card != null)
            card.transform.localScale = new Vector3(width, 1f, 1f) * HeldItem.CardSize;
    }

    private void SetWidth(SpriteRenderer item, float width)
    {
        if (item != null)
            item.transform.localScale = new Vector3(width * itemSize, itemSize, 1f);
    }

    private static void Clear(Transform character)
    {
        if (character == null || !LaidOut.TryGetValue(character, out Lying lying))
            return;

        LaidOut.Remove(character);
        if (lying.Root != null)
            Object.Destroy(lying.Root);
    }

    public override string ToString()
    {
        return way == Way.LayOut
            ? $"{actor} lays out {cards.Count} card{(cards.Count == 1 ? "" : "s")}"
            : $"{actor} picks up what was laid out";
    }
}
