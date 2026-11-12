using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// A chest on the map that Haru opens with the Interact key. The lid flies open and a few random
// cards from its pool come out into the inventory, dealt over Haru as mini cards side by side. Then
// the empty chest fades away, gone for good, also after a reload (CollectedPickups). Chests further
// along the story hold better cards (their pools). An NPC or a thing to pick up in reach answers
// the key first, though not Hideshi following along. The chest is solid (its collider); in reach
// is Haru's feet close to it, from any side. It is there only while its conditions are met: like
// the things on the ground, once Hideshi has shown Haru how to look (a1_ground_revealed). When they
// come true while Haru is there, it comes up out of the grass before his eyes. Before Haru has his
// bags its cards have to fit in his pockets (Pockets): it gives only what fits, and while nothing
// would, it stays shut and its prompt says his pockets are full for a moment.
[RequireComponent(typeof(SpriteRenderer))]
public sealed class LootChest : MonoBehaviour
{
    // From Haru's feet (his position) to the solid part of the chest.
    private const float ReachRadius = 1f;
    // From the top of the chest to the prompt.
    private const float PromptAboveTop = 0.3f;
    private const float PopSeconds = 0.2f;
    private const float PopStretch = 0.15f;
    // The first card shows as the lid settles, the next ones dealt beside it one after the other.
    private const float FirstCardAfter = 0.1f;
    private const float CardInterval = 0.15f;
    // From the middle of one mini card to the next, in units.
    private const float CardSpacing = 0.75f;
    // The open chest stays a moment after the last card, then fades.
    private const float FadeAfter = 0.5f;
    private const float FadeSeconds = 0.6f;
    // Showing up: after a moment, among the things on the ground coming up, from small and
    // see-through to its size, with a little spring (like ForagePickup).
    private const float AppearDelay = 0.5f;
    private const float AppearSeconds = 0.45f;
    // How long the prompt says the pockets are full before it shows the key again.
    private const float FullMessageSeconds = 1.6f;

    [Tooltip("Remembers that it was opened. Unique in the scene. Empty = the object's name.")]
    [SerializeField] private string chestId;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [Tooltip("What can come out. Each card is drawn at random, the same one possibly more than once.")]
    [SerializeField] private List<CardData> cardPool = new();
    [SerializeField, Min(1)] private int minCards = 2;
    [SerializeField, Min(1)] private int maxCards = 3;
    [Tooltip("The prompt shown over the chest in reach (DoorwayPrompt).")]
    [SerializeField] private GameObject promptPrefab;
    [Tooltip("Played as the lid opens.")]
    [SerializeField] private AudioClip openSound;
    [Tooltip("It is there only while all of them are met. Empty = always.")]
    [SerializeField] private List<StoryCondition> conditions = new();

    private static readonly List<LootChest> Placed = new();

    private SpriteRenderer body;
    private Collider2D solid;
    private Transform player;
    private Transform promptRoot;
    private InteractionPrompt prompt;
    private bool promptShown;
    // The prompt says the pockets are full until then (unscaled time); 0 = it shows the key.
    private float fullMessageUntil;
    private bool opened;
    // There, as in the scene, until its conditions say otherwise.
    private bool there = true;
    private Coroutine showingUp;
    private Vector3 restScale;
    private Color restColor;
    // Past Start: from now on it shows up before Haru's eyes instead of just being there.
    private bool started;

    private string ChestId => string.IsNullOrWhiteSpace(chestId) ? name : chestId;

    // A closed chest at Haru's feet, e.g. for Hideshi walking along to leave the key to it.
    public static bool AnyInReach => Placed.Exists(chest => chest.CanBeOpened());

    private void Awake()
    {
        body = GetComponent<SpriteRenderer>();
        solid = GetComponent<Collider2D>();
        restScale = transform.localScale;
        restColor = body.color;
        StoryFlagManager.Changed += Apply;
        QuestManager.QuestChanged += ApplyForQuest;
    }

    private void OnEnable()
    {
        Placed.Add(this);
    }

    private void OnDisable()
    {
        Placed.Remove(this);
    }

    // After the save is loaded: one opened before is gone, and the flags and quests are there.
    private void Start()
    {
        opened = CollectedPickups.IsCollected(ChestId);
        if (opened)
        {
            gameObject.SetActive(false);
            return;
        }

        body.sprite = closedSprite;
        Show(StoryCondition.AreMet(conditions));
        started = true;
    }

    private void ApplyForQuest(QuestData quest, QuestStatus status)
    {
        Apply();
    }

    private void Apply()
    {
        if (started && !opened)
            Show(StoryCondition.AreMet(conditions));
    }

    // There or not. Once past Start it comes up out of the grass instead of just being there.
    private void Show(bool shown)
    {
        if (shown == there)
            return;

        there = shown;
        StopShowingUp();
        if (shown && started)
        {
            showingUp = StartCoroutine(ShowUp());
            return;
        }

        SetThere(shown);
        if (!shown)
            ShowPrompt(false);
    }

    // Seen and solid, or neither.
    private void SetThere(bool shown)
    {
        body.enabled = shown;
        if (solid != null)
            solid.enabled = shown;
    }

    // On the GameClock: Hideshi's lesson, which shows it, is a story event.
    private IEnumerator ShowUp()
    {
        for (float waited = 0f; waited < AppearDelay; waited += GameClock.DeltaTime)
            yield return null;

        SetThere(true);
        for (float elapsed = 0f; elapsed < AppearSeconds; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / AppearSeconds;
            // Ease out with a slight overshoot, then settling at its size.
            float back = t - 1f;
            float size = Mathf.LerpUnclamped(0.3f, 1f, 1f + 2.70158f * back * back * back + 1.70158f * back * back);
            transform.localScale = restScale * size;
            body.color = new Color(restColor.r, restColor.g, restColor.b, restColor.a * Mathf.Clamp01(t * 2f));
            yield return null;
        }

        transform.localScale = restScale;
        body.color = restColor;
        showingUp = null;
    }

    private void StopShowingUp()
    {
        if (showingUp == null)
            return;

        StopCoroutine(showingUp);
        showingUp = null;
        transform.localScale = restScale;
        body.color = restColor;
    }

    private void OnValidate()
    {
        maxCards = Mathf.Max(minCards, maxCards);
    }

    private void OnDestroy()
    {
        StoryFlagManager.Changed -= Apply;
        QuestManager.QuestChanged -= ApplyForQuest;
        if (promptRoot != null)
            Destroy(promptRoot.gameObject);
    }

    private void Update()
    {
        bool canOpen = CanBeOpened() && !NpcInteraction.AnyInRangeBesidesCompanion && !ForagePickup.AnyInReach;
        ShowPrompt(canOpen);
        if (fullMessageUntil > 0f && (!canOpen || Time.unscaledTime >= fullMessageUntil))
            ShowOpenAction();

        if (!canOpen || !GameInputSettings.GetKeyDown(GameInputAction.Interact))
            return;

        if (HasRoomForAny())
            StartCoroutine(Open());
        else
            ShowPocketsFull();
    }

    private static InventoryData Inventory
    {
        get
        {
            PlayerCharacter haru = FindFirstObjectByType<PlayerCharacter>();
            return haru != null ? haru.InventoryData : ExplorationPlayerSession.RuntimeInventory;
        }
    }

    // Something in its pool would fit in Haru's pockets.
    private bool HasRoomForAny()
    {
        InventoryData inventory = Inventory;
        return cardPool.Exists(card => card != null && Pockets.CanTake(inventory, card));
    }

    private bool CanBeOpened()
    {
        return !opened && there && showingUp == null && GameStateManager.IsGameplay && InReach();
    }

    // Measured to the solid part, not the middle: Haru, wide as he is, reaches it from the sides too.
    private bool InReach()
    {
        if (player == null)
        {
            PlayerMovement movement = FindFirstObjectByType<PlayerMovement>();
            player = movement != null ? movement.transform : null;
        }

        if (player == null)
            return false;

        Vector3 feet = player.position;
        if (solid == null)
            return Vector2.Distance(feet, transform.position) <= ReachRadius;

        Bounds bounds = solid.bounds;
        feet.z = bounds.center.z;
        return bounds.SqrDistance(feet) <= ReachRadius * ReachRadius;
    }

    private IEnumerator Open()
    {
        opened = true;
        ShowPrompt(false);
        CollectedPickups.MarkCollected(ChestId);
        AudioManager.TryPlaySfx(openSound);

        // The lid flies open: the open chest, stretched up and springing back.
        body.sprite = openSprite;
        Vector3 scale = transform.localScale;
        for (float elapsed = 0f; elapsed < PopSeconds; elapsed += GameClock.DeltaTime)
        {
            float stretch = PopStretch * Mathf.Sin(elapsed / PopSeconds * Mathf.PI);
            transform.localScale = new Vector3(scale.x * (1f - stretch), scale.y * (1f + stretch), scale.z);
            yield return null;
        }

        transform.localScale = scale;

        PlayerCharacter haru = FindFirstObjectByType<PlayerCharacter>();
        InventoryData inventory = haru != null ? haru.InventoryData : ExplorationPlayerSession.RuntimeInventory;
        List<(CardData card, int count)> found = Draw(inventory);
        float delay = FirstCardAfter;
        for (int i = 0; i < found.Count; i++)
        {
            (CardData card, int count) = found[i];
            inventory?.AddCards(card, count);
            QuestManager.NoteTaken(card, null);
            PickupCard.Show(haru, card, count, delay, (i - (found.Count - 1) * 0.5f) * CardSpacing);
            delay += CardInterval;
        }

        Pockets.NoteTaken(inventory);

        // Empty now: a moment after the last card it fades away, and Haru can walk where it stood.
        yield return new WaitForGameSeconds(delay - CardInterval + FadeAfter);
        if (solid != null)
            solid.enabled = false;

        Color color = body.color;
        for (float elapsed = 0f; elapsed < FadeSeconds; elapsed += GameClock.DeltaTime)
        {
            body.color = new Color(color.r, color.g, color.b, color.a * (1f - elapsed / FadeSeconds));
            yield return null;
        }

        gameObject.SetActive(false);
    }

    // Min to Max cards from the pool, the same ones counted together, in the order they came out;
    // before Haru has his bags, only ones that still fit in his pockets with the rest.
    private List<(CardData card, int count)> Draw(InventoryData inventory)
    {
        List<CardData> pool = cardPool.FindAll(card => card != null);
        List<(CardData card, int count)> found = new();
        HashSet<CardData> taking = new();
        int cards = Random.Range(minCards, Mathf.Max(minCards, maxCards) + 1);
        for (int i = 0; i < cards; i++)
        {
            pool.RemoveAll(fitting => !Pockets.CanTake(inventory, fitting, taking));
            if (pool.Count == 0)
                break;

            CardData card = pool[Random.Range(0, pool.Count)];
            taking.Add(card);
            int index = found.FindIndex(entry => entry.card == card);
            if (index >= 0)
                found[index] = (card, found[index].count + 1);
            else
                found.Add((card, 1));
        }

        return found;
    }

    private void ShowPrompt(bool show)
    {
        if (show == promptShown || (show && !EnsurePrompt()))
            return;

        promptShown = show;
        if (show)
        {
            promptRoot.position = new Vector3(transform.position.x, body.bounds.max.y + PromptAboveTop, transform.position.z);
            ShowOpenAction();
        }

        if (prompt != null)
            prompt.SetShown(show);
        else if (promptRoot != null)
            promptRoot.gameObject.SetActive(show);
    }

    private void ShowOpenAction()
    {
        fullMessageUntil = 0f;
        prompt?.ShowAction(GameInputSettings.GetDisplayName(GameInputAction.Interact), "Open");
    }

    private void ShowPocketsFull()
    {
        if (prompt == null || string.IsNullOrWhiteSpace(Pockets.FullMessage))
            return;

        prompt.ShowMessage(Pockets.FullMessage);
        fullMessageUntil = Time.unscaledTime + FullMessageSeconds;
    }

    // Made the first time Haru comes close; not the chest's child, so the lid's spring leaves it be.
    private bool EnsurePrompt()
    {
        if (promptRoot != null)
            return true;

        if (promptPrefab == null)
            return false;

        promptRoot = Instantiate(promptPrefab).transform;
        promptRoot.name = $"{name}_Prompt";
        prompt = InteractionPrompt.For(promptRoot.GetComponentInChildren<TMP_Text>(true));
        if (prompt != null)
            prompt.SetShown(false);
        else
            promptRoot.gameObject.SetActive(false);

        return true;
    }
}
