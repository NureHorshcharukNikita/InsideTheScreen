using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

// A card in a character's hand turns into what it is, or what they hold turns back into its card, e.g.
// Haru taking out the stick's card to hit Dad with it.
// Take Out Card: the card comes out into the hand and stays there, e.g. for "Uhh... like this?" before
// it turns.
// Card Into Item: the card in the hand (taken out now if there is none) flips, and the thing is in the
// hand (Held Item) until it turns back or the event is over.
// Item Into Card: the thing flips into its card, which goes back into the coat pocket and is gone.
[Serializable, SubclassMenu("Characters/Card Turn")]
public sealed class CardTurnStep : StoryStep
{
    public enum Turn
    {
        CardIntoItem,
        ItemIntoCard,
        TakeOutCard
    }

    private const float PopSeconds = 0.18f;
    private const float FlipSeconds = 0.12f;
    private const float PocketSeconds = 0.35f;

    [SerializeField] private StoryTarget actor = new(StoryTargetType.Player);
    [SerializeField] private Turn turn = Turn.CardIntoItem;
    [SerializeField] private CardData card;
    [Tooltip("What the card is in the hand, e.g. the stick lying on the ground.")]
    [SerializeField] private Sprite item;
    [Tooltip("Which hand: the side this one stands on, e.g. Dad. None = the right hand.")]
    [SerializeField] private StoryTarget towards = new(StoryTargetType.Npc);
    [Tooltip("How long the card is seen before it turns, or after.")]
    [SerializeField, Min(0f)] private float holdSeconds = 0.6f;

    public override IEnumerator Run(StoryEventContext context)
    {
        Transform character = actor.Resolve(context);
        if (character == null)
            yield break;

        context.TakeControl(character);
        // Whatever is still in the hand is gone with the event.
        context.OnRelease(() => HeldItem.PutAway(character));

        SpriteRenderer body = character.GetComponentInChildren<SpriteRenderer>();
        if (body == null)
            yield break;

        switch (turn)
        {
            case Turn.TakeOutCard:
                yield return TakeOutCard(context, character, body, SideOf(character, context));
                break;
            case Turn.CardIntoItem:
                yield return CardIntoItem(context, character, body);
                break;
            default:
                yield return ItemIntoCard(context, character, body);
                break;
        }
    }

    private IEnumerator TakeOutCard(StoryEventContext context, Transform character, SpriteRenderer body, float side)
    {
        GameObject shown = PickupCard.BuildCard(card, body);
        if (shown == null)
            yield break;

        context.OnRelease(() => Remove(shown));

        // Out of the pocket into the hand, with a little overshoot.
        for (float elapsed = 0f; elapsed < PopSeconds; elapsed += GameClock.DeltaTime)
        {
            float pop = elapsed / PopSeconds - 1f;
            Place(shown, body, side, Mathf.LerpUnclamped(0.5f, 1f, 1f + 2.70158f * pop * pop * pop + 1.70158f * pop * pop), 1f);
            yield return null;
        }

        HeldItem.TakeCard(character, shown, side);
        if (holdSeconds > 0f)
            yield return new WaitForGameSeconds(holdSeconds);
    }

    private IEnumerator CardIntoItem(StoryEventContext context, Transform character, SpriteRenderer body)
    {
        HeldItem holding = HeldItem.Of(character);
        if (holding == null || holding.Card == null)
        {
            yield return TakeOutCard(context, character, body, SideOf(character, context));
            holding = HeldItem.Of(character);
        }

        float side = holding != null ? holding.Side : SideOf(character, context);
        GameObject shown = holding != null ? holding.ReleaseCard() : null;
        if (shown != null)
        {
            context.OnRelease(() => Remove(shown));
            yield return Flip(shown, body, side, 1f, 0f);
            Remove(shown);
        }

        HeldItem held = HeldItem.Take(character, item, side);
        if (held == null)
            yield break;

        for (float elapsed = 0f; elapsed < FlipSeconds; elapsed += GameClock.DeltaTime)
        {
            held.Width = elapsed / FlipSeconds;
            yield return null;
        }

        held.Width = 1f;
    }

    private IEnumerator ItemIntoCard(StoryEventContext context, Transform character, SpriteRenderer body)
    {
        HeldItem held = HeldItem.Of(character);
        float side = held != null ? held.Side : SideOf(character, context);
        if (held != null)
        {
            for (float elapsed = 0f; elapsed < FlipSeconds; elapsed += GameClock.DeltaTime)
            {
                held.Width = 1f - elapsed / FlipSeconds;
                yield return null;
            }

            HeldItem.PutAway(character);
        }

        GameObject shown = PickupCard.BuildCard(card, body);
        if (shown == null)
            yield break;

        context.OnRelease(() => Remove(shown));
        yield return Flip(shown, body, side, 0f, 1f);
        for (float elapsed = 0f; elapsed < holdSeconds; elapsed += GameClock.DeltaTime)
        {
            Place(shown, body, side, 1f, 1f);
            yield return null;
        }

        // Into the pocket: from the hand into the coat, smaller and fading.
        SpriteRenderer[] sprites = shown.GetComponentsInChildren<SpriteRenderer>();
        for (float elapsed = 0f; elapsed < PocketSeconds; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / PocketSeconds;
            Place(shown, body, side, Mathf.Lerp(1f, 0.4f, t), 1f, t * t);
            foreach (SpriteRenderer sprite in sprites)
                sprite.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        Remove(shown);
    }

    // Squeezed flat or opened out across, as if turned round in the hand.
    private static IEnumerator Flip(GameObject shown, SpriteRenderer body, float side, float from, float to)
    {
        for (float elapsed = 0f; elapsed < FlipSeconds; elapsed += GameClock.DeltaTime)
        {
            Place(shown, body, side, 1f, Mathf.Lerp(from, to, elapsed / FlipSeconds));
            yield return null;
        }

        Place(shown, body, side, 1f, to);
    }

    // In the hand, held out; Pocketed 0 to 1: on its way into the coat pocket.
    private static void Place(GameObject shown, SpriteRenderer body, float side, float size, float width, float pocketed = 0f)
    {
        if (shown == null || body == null)
            return;

        shown.transform.position = Vector3.Lerp(HeldItem.CardAt(body, side), HeldItem.PocketOf(body), pocketed);
        shown.transform.rotation = Quaternion.Slerp(HeldItem.CardTiltTo(side), Quaternion.identity, pocketed);
        shown.transform.localScale = new Vector3(size * width, size, 1f) * HeldItem.CardSize;
    }

    private float SideOf(Transform character, StoryEventContext context)
    {
        Transform other = towards.type == StoryTargetType.Npc && towards.npc == null ? null : towards.Resolve(context);
        if (other == null || other == character)
            return 1f;

        return other.position.x >= character.position.x ? 1f : -1f;
    }

    private static void Remove(GameObject shown)
    {
        if (shown != null)
            Object.Destroy(shown);
    }

    public override string ToString()
    {
        string what = card != null ? card.CardName : "card";
        return turn switch
        {
            Turn.TakeOutCard => $"{actor} takes out {what}",
            Turn.CardIntoItem => $"{actor}: {what} turns into the thing in hand",
            _ => $"{actor}: the thing in hand turns into {what}"
        };
    }
}
