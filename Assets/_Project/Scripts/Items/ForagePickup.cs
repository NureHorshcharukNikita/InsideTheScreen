using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Something lying on the ground that Haru picks up with the Interact key: a stone, a stick, a
// mushroom (ForageSpawner scatters them, ForageSpot puts one in a set place). Without a sprite of
// its own it is gathered from something drawn in the world, e.g. berries off a bush, which stays.
// In reach - Haru's feet close to it, from any side, from below a bush a little further - the
// prompt shows over the one he is most in reach of. An NPC he looks at answers the key first; one
// beside or behind him, or Hideshi following along, leaves it to the thing at Haru's feet. The
// card goes into the inventory, a mini card of it pops up over Haru
// (PickupCard), and the thing hops and is gone. Before he has his bags it has to fit in his
// pockets (Pockets): if not, it stays and the prompt says his pockets are full for a moment.
public sealed class ForagePickup : MonoBehaviour
{
    // From Haru's feet (his position) to where it lies, unless it says otherwise (a bush, wider).
    private const float ReachRadius = 1.2f;
    private const float PromptHeight = 1.2f;
    private const float PopSeconds = 0.25f;
    private const float PopUpSeconds = 0.4f;
    private const string DefaultAction = "Pick up";
    // How long the prompt says the pockets are full before it shows the key again.
    private const float FullMessageSeconds = 1.6f;

    private static readonly List<ForagePickup> Lying = new();
    private static InteractionPrompt prompt;
    private static Transform promptRoot;
    private static ForagePickup prompted;
    private static int focusFrame = -1;
    private static ForagePickup focused;
    // One press, one thing: the next one in reach waits for the next press, not the same frame.
    private static int pickedFrame = -1;
    // The prompt says the pockets are full until then (unscaled time); 0 = it shows the key.
    private static float fullMessageUntil;

    private CardData card;
    private int count;
    private string pickupId;
    private string action;
    private float reach;
    // How far below it Haru's feet may be: further than to the sides for a bush, whose foot he cannot
    // get right under (its heap's collider), as he can beside it.
    private float reachBelow;
    private Action picked;
    private bool taken;
    // Not come up yet (PopUp after a delay): not seen, not in reach.
    private bool waiting;

    // Made from code (ForageSpawner, ForageSpot, FruitBush): its looks, what it gives, the id that
    // remembers it was taken, the word on the prompt (Pick up when empty) and how far Haru reaches
    // for it (0 = as far as for a thing on the ground), and from below (0 = as far as to the sides).
    public static ForagePickup Create(Transform parent, Vector3 position, Sprite sprite, Material material, CardData card,
        int count, string pickupId, GameObject promptPrefab, Action picked, string action = null, float reach = 0f,
        float reachBelow = 0f)
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
        pickup.reachBelow = reachBelow > 0f ? reachBelow : pickup.reach;
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

        if (fullMessageUntil > 0f && Time.unscaledTime >= fullMessageUntil)
            ShowAction(this);

        if (!GameInputSettings.GetKeyDown(GameInputAction.Interact) || pickedFrame == Time.frameCount)
            return;

        pickedFrame = Time.frameCount;
        if (Pockets.CanTake(Inventory, card))
            StartCoroutine(PickUp());
        else
            ShowPocketsFull();
    }

    private static InventoryData Inventory
    {
        get
        {
            PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
            return player != null ? player.InventoryData : ExplorationPlayerSession.RuntimeInventory;
        }
    }

    // Comes up out of the grass before Haru's eyes (ForageSpot, ForageSpawner, during his father's
    // lesson): after the delay, from small and see-through to its size, with a little spring. On the
    // GameClock, as a story event is playing.
    public void PopUp(float delay = 0f)
    {
        StartCoroutine(PlayPopUp(delay));
    }

    private IEnumerator PlayPopUp(float delay)
    {
        SpriteRenderer look = GetComponent<SpriteRenderer>();
        Vector3 scale = transform.localScale;
        Color color = look.color;
        if (delay > 0f)
        {
            waiting = true;
            look.enabled = false;
            for (float waited = 0f; waited < delay; waited += GameClock.DeltaTime)
                yield return null;

            look.enabled = true;
            waiting = false;
        }

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

    // The thing most in reach, worked out once a frame; none while an NPC Haru looks at, or a menu,
    // has the key.
    private static ForagePickup GetFocused()
    {
        if (focusFrame == Time.frameCount)
            return focused;

        focusFrame = Time.frameCount;
        focused = null;
        PlayerMovement player = GameStateManager.IsGameplay && !NpcInteraction.AnyTakesKeyFromThings
            ? FindFirstObjectByType<PlayerMovement>()
            : null;

        if (player != null)
        {
            float best = float.MaxValue;
            foreach (ForagePickup candidate in Lying)
            {
                if (candidate == null || candidate.taken || candidate.waiting)
                    continue;

                // In reach, and of those the one he is most inside the reach of, e.g. of two bushes
                // side by side the one he stands by.
                float distance = candidate.ReachShare(player.transform.position);
                if (distance <= 1f && distance < best)
                {
                    best = distance;
                    focused = candidate;
                }
            }
        }

        ShowPromptOver(focused);
        return focused;
    }

    // How far Haru's feet are from it, as a share of its reach that way: up to 1 is in reach. Round
    // its point, stretched down by Reach Below.
    private float ReachShare(Vector2 feet)
    {
        Vector2 offset = feet - (Vector2)transform.position;
        float across = offset.x / reach;
        float up = offset.y / (offset.y < 0f ? reachBelow : reach);
        return Mathf.Sqrt(across * across + up * up);
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
            Pockets.NoteTaken(inventory);
            QuestManager.NoteTaken(card, null);
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
            ShowAction(pickup);
        }

        if (prompt != null)
            prompt.SetShown(pickup != null);
        else
            promptRoot.gameObject.SetActive(pickup != null);
    }

    private static void ShowAction(ForagePickup pickup)
    {
        fullMessageUntil = 0f;
        if (prompt != null)
            prompt.ShowAction(GameInputSettings.GetDisplayName(GameInputAction.Interact), pickup.action);
    }

    private static void ShowPocketsFull()
    {
        if (prompt == null || string.IsNullOrWhiteSpace(Pockets.FullMessage))
            return;

        prompt.ShowMessage(Pockets.FullMessage);
        fullMessageUntil = Time.unscaledTime + FullMessageSeconds;
    }
}
