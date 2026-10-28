using System.Collections.Generic;
using UnityEngine;

// A fruit bush Haru picks with the Interact key: one of its cards at random (an apple or berries),
// the fruit gone from the bush at once (Bare Sprite) and the bush shaking as he picks. The fruit
// grows back after a while of exploring (WorldClock), and only while Haru is away from the bush,
// never before his eyes; until then it stays picked, also after a reload. It can be picked only
// while its conditions are met, e.g. the apple bush of Survival Basics once the quest's apple is
// off it (ForageSpot has it before that), or any bush only after Hideshi has shown Haru how to
// gather (a1_ground_lesson). Haru reaches for the middle of the bush, from any side.
[RequireComponent(typeof(SpriteRenderer))]
public sealed class FruitBush : MonoBehaviour
{
    // How far Haru must be from the bush before its fruit grows back.
    private const float RegrowOutOfSightDistance = 16f;
    // From the foot of the bush up to where Haru reaches for the fruit, and how far from his feet.
    private const float ReachHeight = 0.5f;
    private const float Reach = 1.8f;

    [Tooltip("Remembers when it was picked. Unique in the scene. Empty = the object's name.")]
    [SerializeField] private string bushId;
    [Tooltip("One of them at random each time it is picked.")]
    [SerializeField] private List<CardData> cards = new();
    [SerializeField, Min(1)] private int count = 1;
    [Tooltip("The bush once picked, without its fruit.")]
    [SerializeField] private Sprite bareSprite;
    [Tooltip("Seconds of exploring (WorldClock) after which the fruit grows back.")]
    [SerializeField, Min(30f)] private float regrowSeconds = 300f;
    [Tooltip("It can be picked only while all of them are met. Empty = always.")]
    [SerializeField] private List<StoryCondition> conditions = new();
    [Tooltip("The word on the prompt.")]
    [SerializeField] private string action = "Gather";
    [Tooltip("The prompt shown over the bush in reach (DoorwayPrompt).")]
    [SerializeField] private GameObject promptPrefab;

    private SpriteRenderer look;
    private Sprite ripeSprite;
    private ForagePickup fruit;
    private int shownCycle = -1;
    private bool shownAvailable;
    private float nextCheck;

    private string BushId => string.IsNullOrWhiteSpace(bushId) ? name : bushId;

    private int Cycle => Mathf.FloorToInt(WorldClock.PlaySeconds / Mathf.Max(30f, regrowSeconds));

    private void Awake()
    {
        look = GetComponent<SpriteRenderer>();
        ripeSprite = look.sprite;
    }

    // After the save is loaded: picked in this cycle or not.
    private void Start()
    {
        Show();
    }

    // Grown back, or just allowed to be picked: only once Haru is away, checked now and then. Unless
    // the bush looks the same either way, e.g. ripe all along and only now allowed to be picked
    // (after Hideshi's lesson by the bushes): then at once.
    private void Update()
    {
        if (Time.unscaledTime < nextCheck)
            return;

        nextCheck = Time.unscaledTime + 2f;
        bool changed = Cycle != shownCycle || StoryCondition.AreMet(conditions) != shownAvailable;
        if (changed && (!PlayerIsNear() || LooksTheSame()))
            Show();
    }

    // Show would leave the bush as it looks now: not available (nothing to change), or with the
    // fruit it shows already.
    private bool LooksTheSame()
    {
        if (!StoryCondition.AreMet(conditions))
            return true;

        bool ripe = !CollectedPickups.IsCollected($"{BushId}#{Cycle}");
        return (ripe || bareSprite == null ? ripeSprite : bareSprite) == look.sprite;
    }

    private void Show()
    {
        shownCycle = Cycle;
        shownAvailable = StoryCondition.AreMet(conditions);
        string pickId = $"{BushId}#{shownCycle}";
        CollectedPickups.ForgetWhere(id => id.StartsWith(BushId + "#") && id != pickId);

        if (fruit != null)
        {
            Destroy(fruit.gameObject);
            fruit = null;
        }

        // Not ours yet: what it looks like is up to whoever has it (the quest's ForageSpot).
        if (!shownAvailable)
            return;

        bool ripe = !CollectedPickups.IsCollected(pickId);
        look.sprite = ripe || bareSprite == null ? ripeSprite : bareSprite;
        CardData card = PickCard(shownCycle);
        if (ripe && card != null)
        {
            fruit = ForagePickup.Create(transform, transform.position + Vector3.up * ReachHeight, null, null, card,
                count, pickId, promptPrefab, Picked, action, Reach);
        }
    }

    // The same one for the whole cycle, also after a reload.
    private CardData PickCard(int cycle)
    {
        List<CardData> options = cards.FindAll(card => card != null);
        if (options.Count == 0)
            return null;

        unchecked
        {
            int hash = 23;
            foreach (char character in BushId)
                hash = hash * 31 + character;
            return options[new System.Random(hash ^ (cycle * 7919)).Next(options.Count)];
        }
    }

    private void Picked()
    {
        fruit = null;
        if (bareSprite != null)
            look.sprite = bareSprite;
        StartCoroutine(ForageSpot.Rustle(transform));
    }

    private bool PlayerIsNear()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        return player != null
            && Vector2.Distance(player.transform.position, transform.position) < RegrowOutOfSightDistance;
    }
}
