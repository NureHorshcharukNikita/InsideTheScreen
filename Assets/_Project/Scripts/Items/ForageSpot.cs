using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// One thing to gather in a set place, e.g. by the apple bushes near the tower for Survival Basics:
// a stone or a stick lying there (Sprite), or an apple off a bush drawn in the world (no Sprite: the
// bush stays, the prompt shows over it). Picked up like the forage of a ForageSpawner, but it does
// not grow back. It is there while its conditions are met (Haru has been shown how to look) and
// until it is taken. When its conditions come true while Haru is there, a thing lying on the ground
// shows up before his eyes, out of the grass, each after its own delay, so several come one after
// another. Picked off a bush, the fruit is gone from it (Drawn On Picked) and the bush shakes, as
// Haru picks; from then on the bush's FruitBush grows it back. Taking it sets its story flags, for a
// story event to follow it (Haru's first find) or to count what is gathered.
public sealed class ForageSpot : MonoBehaviour
{
    private const float RustleSeconds = 0.5f;
    private const float RustleWidth = 0.07f;

    [Tooltip("Remembers that it was taken. Unique in the scene. Empty = the object's name.")]
    [SerializeField] private string pickupId;
    [SerializeField] private CardData card;
    [SerializeField, Min(1)] private int count = 1;
    [Tooltip("How it lies on the ground. Empty = gathered from something drawn here (a bush), which stays.")]
    [SerializeField] private Sprite sprite;
    [Tooltip("No Sprite: what it is gathered from, e.g. the apple bush, which shakes as Haru picks.")]
    [SerializeField] private Transform drawnOn;
    [Tooltip("What that looks like once picked, e.g. the bush without its apples.")]
    [SerializeField] private Sprite drawnOnPicked;
    [Tooltip("The word on the prompt, e.g. Pick up or Gather.")]
    [SerializeField] private string action = "Pick up";
    [Tooltip("Set when it is taken.")]
    [SerializeField] private List<string> flagsWhenTaken = new();
    [Tooltip("It is there only while all of them are met. Empty = always.")]
    [SerializeField] private List<StoryCondition> conditions = new();
    [Tooltip("Showing up before Haru's eyes: after how long, in seconds.")]
    [SerializeField, Min(0f)] private float appearDelay;
    [Tooltip("The lit sprite material, so it darkens at night like the rest of the world.")]
    [SerializeField] private Material material;
    [Tooltip("The prompt shown over the thing in reach (DoorwayPrompt).")]
    [SerializeField] private GameObject promptPrefab;

    private ForagePickup lying;
    private Coroutine showingUp;
    // Past Start: from now on it shows up before Haru's eyes instead of just being there.
    private bool started;

    private string PickupId => string.IsNullOrWhiteSpace(pickupId) ? name : pickupId;

    public bool SetsFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && flagsWhenTaken.Contains(flag);
    }

    // As if Haru had picked it up, without the hop and the text (Debug Test Start): the card, the
    // pick remembered, the flags.
    public void Skip()
    {
        if (CollectedPickups.IsCollected(PickupId))
            return;

        CollectedPickups.MarkCollected(PickupId);
        PlayerCharacter player = FindFirstObjectByType<PlayerCharacter>();
        InventoryData inventory = player != null ? player.InventoryData : ExplorationPlayerSession.RuntimeInventory;
        if (card != null && inventory != null)
            inventory.AddCards(card, count);

        Hide();
        Taken();
        ShowPicked();
    }

    private void Awake()
    {
        StoryFlagManager.Changed += Apply;
        QuestManager.QuestChanged += ApplyForQuest;
    }

    // After the save is loaded: which things are taken, which flags and quests are there. A bush
    // picked before is left to its FruitBush, which grows the fruit back.
    private void Start()
    {
        Apply();
        started = true;
    }

    private void OnDestroy()
    {
        StoryFlagManager.Changed -= Apply;
        QuestManager.QuestChanged -= ApplyForQuest;
    }

    private void ApplyForQuest(QuestData quest, QuestStatus status)
    {
        Apply();
    }

    private void Apply()
    {
        bool there = card != null && !CollectedPickups.IsCollected(PickupId) && StoryCondition.AreMet(conditions);
        if (!there)
            Hide();
        else if (lying == null && showingUp == null)
        {
            if (started)
                showingUp = StartCoroutine(ShowUp());
            else
                lying = Create();
        }
    }

    private IEnumerator ShowUp()
    {
        if (appearDelay > 0f)
            yield return new WaitForGameSeconds(appearDelay);

        lying = Create();
        showingUp = null;
        if (sprite != null)
            lying.PopUp();
    }

    // Taken by Haru. Off a bush, the fruit is gone from it at once, then it shakes as he picks.
    private void PickedUp()
    {
        Taken();
        ShowPicked();
        if (drawnOn != null)
            StartCoroutine(Rustle(drawnOn));
    }

    private void ShowPicked()
    {
        if (drawnOn != null && drawnOnPicked != null && drawnOn.TryGetComponent(out SpriteRenderer bush))
            bush.sprite = drawnOnPicked;
    }

    // A few quick shakes, wide and settling, the bush keeping its foot on the ground (also FruitBush).
    // On the GameClock: the first find starts a story event, which pauses the game.
    public static IEnumerator Rustle(Transform bush)
    {
        Vector3 scale = bush.localScale;
        for (float elapsed = 0f; elapsed < RustleSeconds && bush != null; elapsed += GameClock.DeltaTime)
        {
            float t = elapsed / RustleSeconds;
            float shake = RustleWidth * Mathf.Sin(t * 3f * Mathf.PI * 2f) * (1f - t);
            bush.localScale = new Vector3(scale.x * (1f + shake), scale.y * (1f - shake * 0.6f), scale.z);
            yield return null;
        }

        if (bush != null)
            bush.localScale = scale;
    }

    private ForagePickup Create()
    {
        // Off a bush: reached as the bush's own fruit is, from below a little further.
        bool offBush = drawnOn != null && drawnOn.GetComponent<FruitBush>() != null;
        return ForagePickup.Create(transform, transform.position, sprite, material, card, count, PickupId,
            promptPrefab, PickedUp, action, offBush ? FruitBush.Reach : 0f, offBush ? FruitBush.ReachBelow : 0f);
    }

    private void Hide()
    {
        if (showingUp != null)
        {
            StopCoroutine(showingUp);
            showingUp = null;
        }

        if (lying != null)
        {
            Destroy(lying.gameObject);
            lying = null;
        }
    }

    private void Taken()
    {
        // It hops and is gone by itself; the flags below must not take it away halfway.
        lying = null;
        SetTakenFlags(flagsWhenTaken);
    }

    // The story flags of something taken, and the quests counting them get to know (also FruitBush,
    // ForageSpawner: any first find, e.g., Haru's "it turned into a card?").
    public static void SetTakenFlags(List<string> flags)
    {
        if (flags == null || flags.Count == 0)
            return;

        foreach (string flag in flags)
        {
            if (!string.IsNullOrWhiteSpace(flag))
                StoryFlagManager.SetFlag(flag);
        }

        QuestManager.NoteTaken(null, flags);
    }
}
