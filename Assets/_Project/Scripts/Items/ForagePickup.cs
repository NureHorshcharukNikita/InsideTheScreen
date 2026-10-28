using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Something lying on the ground that Haru picks up with the Interact key: a stone, a stick, a
// mushroom (ForageSpawner scatters them, ForageSpot puts one in a set place). Without a sprite of
// its own it is gathered from something drawn in the world, e.g. berries off a bush, which stays.
// In reach - Haru's feet close to it, from any side - the prompt shows over the closest. An NPC in
// reach answers the key first, except Hideshi following along: walking together, the thing at
// Haru's feet comes first. The card goes into the inventory, a mini card of it pops up over Haru
// (PickupCard), and the thing hops and is gone.
public sealed class ForagePickup : MonoBehaviour
{
    // From Haru's feet (his position) to where it lies, unless it says otherwise (a bush, wider).
    private const float ReachRadius = 1.2f;
    private const float PromptHeight = 1.2f;
    private const float PopSeconds = 0.25f;
    private const float PopUpSeconds = 0.4f;
    private const string DefaultAction = "Pick up";

    private static readonly List<ForagePickup> Lying = new();
    private static InteractionPrompt prompt;
    private static Transform promptRoot;
    private static ForagePickup prompted;
    private static int focusFrame = -1;
    private static ForagePickup focused;

    private CardData card;
    private int count;
    private string pickupId;
    private string action;
    private float reach;
    private Action picked;
    private bool taken;

    // Made from code (ForageSpawner, ForageSpot, FruitBush): its looks, what it gives, the id that
    // remembers it was taken, the word on the prompt (Pick up when empty) and how far Haru reaches
    // for it (0 = as far as for a thing on the ground).
    public static ForagePickup Create(Transform parent, Vector3 position, Sprite sprite, Material material, CardData card,
        int count, string pickupId, GameObject promptPrefab, Action picked, string action = null, float reach = 0f)
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

        ForagePickup pickup = thing.AddComponent<ForagePickup>();
        pickup.card = card;
        pickup.count = Mathf.Max(1, count);
        pickup.pickupId = pickupId;
        pickup.action = string.IsNullOrWhiteSpace(action) ? DefaultAction : action;
        pickup.reach = reach > 0f ? reach : ReachRadius;
        pickup.picked = picked;
        EnsurePrompt(promptPrefab);
        return pickup;
    }

    private void OnEnable()
    {
        Lying.Add(this);
    }

    private void OnDisable()
    {
        Lying.Remove(this);
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

    // Comes up out of the grass before Haru's eyes (ForageSpot, during his father's lesson): from small
    // and see-through to its size, with a little spring. On the GameClock, as a story event is playing.
    public void PopUp()
    {
        StartCoroutine(PlayPopUp());
    }

    private IEnumerator PlayPopUp()
    {
        SpriteRenderer look = GetComponent<SpriteRenderer>();
        Vector3 scale = transform.localScale;
        Color color = look.color;
        for (float elapsed = 0f; elapsed < PopUpSeconds; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / PopUpSeconds;
            // Ease out with a slight overshoot, then settling at its size.
            float back = t - 1f;
            float size = Mathf.LerpUnclamped(0.3f, 1f, 1f + 2.70158f * back * back * back + 1.70158f * back * back);
            transform.localScale = scale * size;
            look.color = new Color(color.r, color.g, color.b, color.a * Mathf.Clamp01(t * 2f));
            yield return null;
        }

        transform.localScale = scale;
        look.color = color;
    }

    // Something to pick up has the key, e.g. for a chest close by to leave it be.
    public static bool AnyInReach => GetFocused() != null;

    // The closest thing in reach, worked out once a frame; none while an NPC other than Hideshi
    // following along, or a menu, has the key.
    private static ForagePickup GetFocused()
    {
        if (focusFrame == Time.frameCount)
            return focused;

        focusFrame = Time.frameCount;
        focused = null;
        PlayerMovement player = GameStateManager.IsGameplay && !NpcInteraction.AnyInRangeBesidesCompanion
            ? FindFirstObjectByType<PlayerMovement>()
            : null;

        if (player != null)
        {
            float best = float.MaxValue;
            foreach (ForagePickup candidate in Lying)
            {
                if (candidate == null || candidate.taken)
                    continue;

                float distance = Vector2.Distance(candidate.transform.position, player.transform.position);
                if (distance <= candidate.reach && distance < best)
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
        ShowPromptOver(null);
        focusFrame = -1;
        CollectedPickups.MarkCollected(pickupId);

        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        InventoryData inventory = player != null ? player.InventoryData : ExplorationPlayerSession.RuntimeInventory;
        if (card != null && inventory != null)
        {
            inventory.AddCards(card, count);
            PickupCard.Show(player, card, count);
        }

        picked?.Invoke();

        // A little hop up into Haru's hands, then gone. On the GameClock, like the mini card: a story
        // event the pick starts (Haru's first find) pauses the game, and they still play out under it.
        Vector3 ground = transform.position;
        Vector3 scale = transform.localScale;
        for (float elapsed = 0f; elapsed < PopSeconds; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / PopSeconds;
            transform.position = ground + Vector3.up * (0.5f * Mathf.Sin(t * Mathf.PI));
            transform.localScale = scale * (1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }


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
                prompt.ShowAction(GameInputSettings.GetDisplayName(GameInputAction.Interact), pickup.action);
        }

        if (prompt != null)
            prompt.SetShown(pickup != null);
        else
            promptRoot.gameObject.SetActive(pickup != null);
    }
}
