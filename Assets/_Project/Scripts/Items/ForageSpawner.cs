using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Things lying on the ground for Haru to pick up - stones, sticks, mushrooms, herbs - scattered over
// an area (the BoxCollider2D on this object marks it and is only used for that). Each spot holds one
// thing, put at random on open grass or path: not in water, on cliffs, in trees or against walls.
// Picked ones grow back, all of the area's spots at once and in new places, after a while of
// exploring (WorldClock); that happens while the player is away from the area, never before their
// eyes. The spots stay put between visits, and a picked one stays picked until then. The things lie
// there only while the conditions are met, e.g. once Hideshi has shown Haru how to look
// (a1_ground_revealed); when they come true, the things come up out of the grass one after another.
// Picking one sets the area's story flags, e.g. a1_gathered_first for Haru's first find.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class ForageSpawner : MonoBehaviour
{
    [Serializable]
    private sealed class Forage
    {
        public CardData card;
        [Tooltip("How it lies on the ground.")]
        public Sprite sprite;
        [Min(1)] public int count = 1;
        [Tooltip("Higher = likelier.")]
        [Min(1)] public int weight = 1;
    }

    private const float ClearRadius = 0.45f;
    private const int TriesPerSpot = 25;
    // How far the player must be from the area before its things grow back.
    private const float RegrowOutOfSightDistance = 16f;
    // Coming up out of the grass: each thing after its own delay, up to this, in seconds.
    private const float MaxAppearDelay = 1.2f;

    [Tooltip("Remembers which spots are picked. Unique in the scene.")]
    [SerializeField] private string spawnerId;
    [SerializeField] private List<Forage> forage = new();
    [SerializeField, Min(1)] private int spots = 6;
    [Tooltip("Seconds of exploring (WorldClock) after which picked things grow back.")]
    [SerializeField, Min(30f)] private float regrowSeconds = 240f;
    [Tooltip("The lit sprite material, so the things darken at night like the rest of the world.")]
    [SerializeField] private Material material;
    [Tooltip("The prompt shown over the thing in reach (DoorwayPrompt).")]
    [SerializeField] private GameObject promptPrefab;
    [Tooltip("The things lie there only while all of them are met. Empty = always.")]
    [SerializeField] private List<StoryCondition> conditions = new();
    [Tooltip("Set each time a thing here is picked, e.g. a1_gathered_first for Haru's first find. Empty = none.")]
    [SerializeField] private List<string> flagsWhenTaken = new();

    private static readonly List<Tilemap> OpenGround = new();
    private static readonly List<Tilemap> Blocked = new();
    private static int tilemapsFoundForScene = -1;

    private readonly List<ForagePickup> lying = new();
    private BoxCollider2D area;
    private int shownCycle = -1;
    private bool shownAvailable;
    private float nextCheck;
    // Past Start: from now on the things come up before Haru's eyes instead of just lying there.
    private bool started;

    private int Cycle => Mathf.FloorToInt(WorldClock.PlaySeconds / Mathf.Max(30f, regrowSeconds));

    private void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;
        if (string.IsNullOrWhiteSpace(spawnerId))
            spawnerId = name;

        StoryFlagManager.Changed += Apply;
        QuestManager.QuestChanged += ApplyForQuest;
    }

    // After the save is loaded: which spots are picked, which flags and quests are there.
    private void Start()
    {
        FindTilemaps();
        Spread(Cycle, false);
        started = true;
    }

    private void OnDestroy()
    {
        StoryFlagManager.Changed -= Apply;
        QuestManager.QuestChanged -= ApplyForQuest;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheck)
            return;

        nextCheck = Time.unscaledTime + 2f;
        if (Cycle != shownCycle && !PlayerIsNear())
            Spread(Cycle, false);
    }

    private void ApplyForQuest(QuestData quest, QuestStatus status)
    {
        Apply();
    }

    // Allowed now, or no longer: at once, the same spots as before (the cycle grows back by itself).
    private void Apply()
    {
        if (started && StoryCondition.AreMet(conditions) != shownAvailable)
            Spread(shownCycle, true);
    }

    // The things of this cycle that are not picked yet, each on its own spot, if they may lie there.
    // Popping up: coming up out of the grass, each after its own delay.
    private void Spread(int cycle, bool popUp)
    {
        foreach (ForagePickup thing in lying)
        {
            if (thing != null)
                Destroy(thing.gameObject);
        }

        lying.Clear();
        shownCycle = cycle;
        shownAvailable = StoryCondition.AreMet(conditions);

        string current = $"{spawnerId}#{cycle}#";
        CollectedPickups.ForgetWhere(id => id.StartsWith(spawnerId + "#") && !id.StartsWith(current));
        if (forage.Count == 0 || !shownAvailable)
            return;

        // The same places and things for the whole cycle, also after a battle or a reload.
        System.Random random = new(StableHash(spawnerId) ^ (cycle * 7919));
        for (int spot = 0; spot < spots; spot++)
        {
            Forage pick = PickForage(random);
            bool found = TryFindPlace(random, out Vector2 place);
            string pickupId = current + spot;
            if (!found || pick?.card == null || pick.sprite == null || CollectedPickups.IsCollected(pickupId))
                continue;

            ForagePickup thing = ForagePickup.Create(transform, place, pick.sprite, material, pick.card, pick.count, pickupId, promptPrefab, Taken);
            if (popUp)
                thing.PopUp(UnityEngine.Random.Range(0f, MaxAppearDelay));

            lying.Add(thing);
        }
    }

    private void Taken()
    {
        ForageSpot.SetTakenFlags(flagsWhenTaken);
    }

    private Forage PickForage(System.Random random)
    {
        int total = 0;
        foreach (Forage option in forage)
            total += option != null ? Mathf.Max(1, option.weight) : 0;

        int roll = random.Next(Mathf.Max(1, total));
        foreach (Forage option in forage)
        {
            if (option == null)
                continue;

            roll -= Mathf.Max(1, option.weight);
            if (roll < 0)
                return option;
        }

        return null;
    }

    private bool TryFindPlace(System.Random random, out Vector2 place)
    {
        Bounds bounds = area.bounds;
        for (int i = 0; i < TriesPerSpot; i++)
        {
            place = new Vector2(
                Mathf.Lerp(bounds.min.x, bounds.max.x, (float)random.NextDouble()),
                Mathf.Lerp(bounds.min.y, bounds.max.y, (float)random.NextDouble()));
            if (IsOpenGround(place))
                return true;
        }

        place = default;
        return false;
    }

    private static bool IsOpenGround(Vector2 place)
    {
        foreach (Tilemap tilemap in Blocked)
        {
            if (tilemap != null && tilemap.HasTile(tilemap.WorldToCell(place)))
                return false;
        }

        bool onGround = false;
        foreach (Tilemap tilemap in OpenGround)
            onGround |= tilemap != null && tilemap.HasTile(tilemap.WorldToCell(place));

        if (!onGround)
            return false;

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(place, ClearRadius))
        {
            if (!hit.isTrigger)
                return false;
        }

        return true;
    }

    // Grass and path to lie on; cliffs, water and trees to keep off, by the names of the tilemaps.
    private static void FindTilemaps()
    {
        int scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (tilemapsFoundForScene == scene)
            return;

        tilemapsFoundForScene = scene;
        OpenGround.Clear();
        Blocked.Clear();
        foreach (Tilemap tilemap in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
        {
            string tilemapName = tilemap.name.ToLowerInvariant();
            string parentName = tilemap.transform.parent != null ? tilemap.transform.parent.name.ToLowerInvariant() : string.Empty;
            string both = tilemapName + " " + parentName;
            if (both.Contains("cliff") || both.Contains("water") || both.Contains("tree") || both.Contains("bridge"))
                Blocked.Add(tilemap);
            else if (both.Contains("grass") || both.Contains("path"))
                OpenGround.Add(tilemap);
        }
    }

    private bool PlayerIsNear()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        return player != null && area.bounds.SqrDistance(player.transform.position) < RegrowOutOfSightDistance * RegrowOutOfSightDistance;
    }

    private static int StableHash(string text)
    {
        unchecked
        {
            int hash = 23;
            foreach (char character in text)
                hash = hash * 31 + character;
            return hash;
        }
    }
}
