using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Something lying on the ground that Haru picks up with the Interact key: a stone, a stick, a
// mushroom (ForageSpawner scatters them). Near one, the prompt shows over the closest; an NPC in
// reach answers the key first. The card goes into the inventory (and the deck, if there is room),
// with a "+ Card" text over Haru, and the thing hops and is gone.
public sealed class ForagePickup : MonoBehaviour
{
    private const float ReachRadius = 1.1f;
    private const float PromptHeight = 1.2f;
    private const float PopSeconds = 0.25f;
    private const float TextLifetime = 1.2f;

    private static readonly List<ForagePickup> InReach = new();
    private static InteractionPrompt prompt;
    private static Transform promptRoot;
    private static ForagePickup prompted;
    private static int focusFrame = -1;
    private static ForagePickup focused;

    private CardData card;
    private int count;
    private string pickupId;
    private Action picked;
    private bool taken;

    // Made from code (ForageSpawner): its looks, what it gives, the id that remembers it was taken.
    public static ForagePickup Create(Transform parent, Vector3 position, Sprite sprite, Material material, CardData card,
        int count, string pickupId, GameObject promptPrefab, Action picked)
    {
        GameObject thing = new($"Forage_{(card != null ? card.name : "Thing")}");
        thing.transform.SetParent(parent, false);
        thing.transform.position = position;

        SpriteRenderer renderer = thing.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        // Lit like the rest of the world, so it darkens at night.
        if (material != null)
            renderer.sharedMaterial = material;

        CircleCollider2D reach = thing.AddComponent<CircleCollider2D>();
        reach.isTrigger = true;
        reach.radius = ReachRadius;

        ForagePickup pickup = thing.AddComponent<ForagePickup>();
        pickup.card = card;
        pickup.count = Mathf.Max(1, count);
        pickup.pickupId = pickupId;
        pickup.picked = picked;
        EnsurePrompt(promptPrefab);
        return pickup;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !InReach.Contains(this))
            InReach.Add(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            InReach.Remove(this);
    }

    private void OnDisable()
    {
        InReach.Remove(this);
        if (prompted == this)
            ShowPromptOver(null);
    }

    private void Update()
    {
        if (GetFocused() != this || taken)
            return;

        if (GameInputSettings.GetKeyDown(GameInputAction.Interact))
            StartCoroutine(PickUp());
    }

    // The closest thing in reach, worked out once a frame; none while an NPC or a menu has the key.
    private static ForagePickup GetFocused()
    {
        if (focusFrame == Time.frameCount)
            return focused;

        focusFrame = Time.frameCount;
        focused = null;
        PlayerMovement player = GameStateManager.IsGameplay && !NpcInteraction.AnyInRange
            ? FindFirstObjectByType<PlayerMovement>()
            : null;

        if (player != null)
        {
            float best = float.MaxValue;
            foreach (ForagePickup candidate in InReach)
            {
                float distance = candidate != null && !candidate.taken
                    ? Vector2.Distance(candidate.transform.position, player.transform.position)
                    : float.MaxValue;
                if (distance < best)
                {
                    best = distance;
                    focused = candidate;
                }
            }
        }

        ShowPromptOver(focused);
        return focused;
    }

    private IEnumerator PickUp()
    {
        taken = true;
        InReach.Remove(this);
        ShowPromptOver(null);
        focusFrame = -1;
        CollectedPickups.MarkCollected(pickupId);

        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        InventoryData inventory = player != null ? player.InventoryData : ExplorationPlayerSession.RuntimeInventory;
        if (card != null && inventory != null)
        {
            inventory.AddCards(card, count);
            ExplorationPlayerSession.AddToDeckIfRoom(card, count);
            if (player != null)
                ShowPickedText(player, count > 1 ? $"+ {CardName} x{count}" : $"+ {CardName}");
        }

        picked?.Invoke();

        // A little hop up into Haru's hands, then gone.
        Vector3 ground = transform.position;
        Vector3 scale = transform.localScale;
        for (float elapsed = 0f; elapsed < PopSeconds; elapsed += Time.deltaTime)
        {
            float t = elapsed / PopSeconds;
            transform.position = ground + Vector3.up * (0.5f * Mathf.Sin(t * Mathf.PI));
            transform.localScale = scale * (1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private string CardName => card == null ? "Card" : string.IsNullOrWhiteSpace(card.CardName) ? card.name : card.CardName;

    private static void EnsurePrompt(GameObject promptPrefab)
    {
        if (promptRoot != null || promptPrefab == null)
            return;

        promptRoot = Instantiate(promptPrefab).transform;
        promptRoot.name = "ForagePrompt";
        TMP_Text text = promptRoot.GetComponentInChildren<TMP_Text>(true);
        prompt = InteractionPrompt.For(text);
        if (prompt != null)
            prompt.SetShown(false);
        else
            promptRoot.gameObject.SetActive(false);
    }

    private static void ShowPromptOver(ForagePickup pickup)
    {
        if (promptRoot == null || pickup == prompted)
            return;

        prompted = pickup;
        if (pickup != null)
        {
            promptRoot.position = pickup.transform.position + Vector3.up * PromptHeight;
            if (prompt != null)
                prompt.ShowAction(GameInputSettings.GetDisplayName(GameInputAction.Interact), "Pick up");
        }

        if (prompt != null)
            prompt.SetShown(pickup != null);
        else
            promptRoot.gameObject.SetActive(pickup != null);
    }

    // Like the text of the other pickups (RandomCardPickup): rises over Haru and fades.
    private static void ShowPickedText(PlayerCharacter player, string message)
    {
        GameObject textObject = new("PickupCardText");
        textObject.transform.SetParent(player.transform, false);
        textObject.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = message;
        text.fontSize = 3f;
        text.alignment = TextAlignmentOptions.Center;
        text.sortingOrder = 100;
        player.StartCoroutine(FadeText(text));
    }

    private static IEnumerator FadeText(TextMeshPro text)
    {
        Vector3 start = text.transform.localPosition;
        for (float elapsed = 0f; elapsed < TextLifetime && text != null; elapsed += Time.deltaTime)
        {
            float t = elapsed / TextLifetime;
            text.transform.localPosition = start + Vector3.up * (0.5f * t);
            text.alpha = 1f - t;
            yield return null;
        }

        if (text != null)
            Destroy(text.gameObject);
    }
}
