using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// An enemy on the map: walking into it starts a battle against it, and against whoever is with it:
// a few more enemies may join, rolled at the first battle and the same ones every time after
// (EncounterGroups). The map only ever shows this one. Companions following the player fight at
// the player's side. One that respawns waits out of sight once beaten and comes back on its own:
// after a few won battles, or after a while of exploring, so a cleared forest fills up again.
public class EnemyEncounter : MonoBehaviour
{
    // In a saved group: one more of the enemy itself, not one of Possible Extra Enemies.
    public const int SameEnemy = -1;

    private const int MaxExtraEnemies = PendingBattleEnemy.MaxPerSide - 1;
    // How far (world units) the touch zone reaches past the feet collider on every side.
    private const float TouchMargin = 0.25f;
    // How far short of a wall the player is put back after the battle.
    private const float WallGap = 0.05f;

    private static readonly RaycastHit2D[] ReturnHits = new RaycastHit2D[8];

    [SerializeField] private EnemyData enemyData;

    [Header("Group")]
    [Tooltip("How many more enemies join it at least, rolled at the first battle.")]
    [SerializeField, Range(0, MaxExtraEnemies)] private int minExtraEnemies;
    [Tooltip("How many more enemies join it at most (three a side).")]
    [SerializeField, Range(0, MaxExtraEnemies)] private int maxExtraEnemies;
    [Tooltip("Who may join, picked at random for each place. List one several times to make it likelier. Empty = more of the same enemy.")]
    [SerializeField] private List<EnemyData> possibleExtraEnemies = new();

    [Space]
    [SerializeField] private string encounterId;
    [Tooltip("How far the player is put back from it after the battle; a wall in the way stops him short of it.")]
    [SerializeField] private float playerReturnOffset = 1.25f;

    [Header("Respawn")]
    [Tooltip("Comes back after the player has won Respawn After Battles other battles or explored for Respawn After Seconds, so there is always something to hunt for loot and quests. Off for bosses and quest enemies: they stay beaten.")]
    [SerializeField] private bool respawns;
    [SerializeField, Min(1)] private int respawnAfterBattles = 3;
    [Tooltip("Seconds of exploring (WorldClock, not counting battles and menus) after which it comes back even without other battles.")]
    [SerializeField, Min(10f)] private float respawnAfterSeconds = 180f;

    private static int nextEncounterId = 1;

    // It does not come back right next to the player.
    private const float ComeBackDistance = 6f;
    private const float ComeBackCheckInterval = 1f;
    private const float ComeBackFadeSeconds = 0.6f;

    private bool isTriggered = false;
    private bool away;
    private float nextComeBackCheck;

    public static void ResetEncounterIds()
    {
        nextEncounterId = 1;
    }

    private void Awake()
    {
        MapSprite.Apply(gameObject, enemyData != null ? enemyData.MapSprite : null);

        if (string.IsNullOrWhiteSpace(encounterId))
            encounterId = (nextEncounterId++).ToString();
    }

    private void Start()
    {
        if (DefeatedEncounters.IsDefeated(encounterId))
        {
            if (!respawns)
            {
                Destroy(gameObject);
                return;
            }

            if (!IsDueBack())
            {
                SetAway(true);
                return;
            }

            // Back again: a new group is rolled, like at the first meeting.
            DefeatedEncounters.Forget(encounterId);
            EncounterGroups.Forget(encounterId);
        }

        GrowTouchZone();
    }

    private void Update()
    {
        if (!away || Time.unscaledTime < nextComeBackCheck)
            return;

        nextComeBackCheck = Time.unscaledTime + ComeBackCheckInterval;
        if (IsDueBack() && GameStateManager.IsGameplay && !PlayerIsNear())
            StartCoroutine(ComeBack());
    }

    // An enemy that sprang out of the bushes on a forest road (AmbushZone): its own id, and who is
    // with it, already picked. It does not come back once beaten.
    public void SetUpAmbush(string id, IReadOnlyList<EnemyData> group)
    {
        encounterId = id;
        respawns = false;
        possibleExtraEnemies = new List<EnemyData>(group);
        int[] extraEnemies = new int[possibleExtraEnemies.Count];
        for (int i = 0; i < extraEnemies.Length; i++)
            extraEnemies[i] = i;

        EncounterGroups.Remember(id, extraEnemies);
    }

    public EnemyData Data => enemyData;

    private bool IsDueBack()
    {
        return DefeatedEncounters.BattlesSince(encounterId) >= respawnAfterBattles
            || DefeatedEncounters.SecondsSince(encounterId) >= respawnAfterSeconds;
    }

    private bool PlayerIsNear()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        return player != null && Vector2.Distance(player.transform.position, transform.position) < ComeBackDistance;
    }

    // Beaten: out of sight and out of the way until it comes back.
    private void SetAway(bool isAway)
    {
        away = isAway;
        foreach (Renderer part in GetComponentsInChildren<Renderer>(true))
            part.enabled = !isAway;
        foreach (Collider2D part in GetComponentsInChildren<Collider2D>(true))
            part.enabled = !isAway;
        if (TryGetComponent(out EnemyChaser chaser))
            chaser.enabled = !isAway;
    }

    private IEnumerator ComeBack()
    {
        DefeatedEncounters.Forget(encounterId);
        EncounterGroups.Forget(encounterId);
        SetAway(false);
        GrowTouchZone();

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite == null)
            yield break;

        Color shown = sprite.color;
        for (float elapsed = 0f; elapsed < ComeBackFadeSeconds; elapsed += Time.deltaTime)
        {
            sprite.color = new Color(shown.r, shown.g, shown.b, shown.a * elapsed / ComeBackFadeSeconds);
            yield return null;
        }

        sprite.color = shown;
    }

    // The trigger that starts the battle reaches a little past the feet collider. The same size or
    // smaller, it never fires: the player and an enemy running at them press against each other's
    // feet colliders and stop there, without ever overlapping.
    // In Start, after BoxCollider2DSpriteSync has fitted the feet collider to the sprite.
    private void GrowTouchZone()
    {
        BoxCollider2D feet = null;
        BoxCollider2D touchZone = null;
        foreach (BoxCollider2D box in GetComponents<BoxCollider2D>())
        {
            if (box.isTrigger)
                touchZone ??= box;
            else
                feet ??= box;
        }

        if (touchZone == null || feet == null)
            return;

        Vector3 scale = transform.lossyScale;
        Vector2 margin = new(
            TouchMargin * 2f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            TouchMargin * 2f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)));
        touchZone.offset = feet.offset;
        touchZone.size = feet.size + margin;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            isTriggered = true;
            ExplorationPlayerSession.SavePosition(GetPlayerReturnPosition(other));
            ExplorationPlayerSession.SaveNpcPositions();

            PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                playerMovement.StopMovement();
                playerMovement.enabled = false;
            }

            PendingBattleEnemy.RegisterEncounterStart(GetEnemies(), CompanionFollower.CollectBattleAllies(), encounterId);
            FadeManager.TryFadeToScene(SceneNames.Battle);
        }
    }

    private void OnValidate()
    {
        maxExtraEnemies = Mathf.Max(maxExtraEnemies, minExtraEnemies);
        // The look comes from the data: a new enemy is a variant of ExplorationEnemyBase with its EnemyData.
        MapSprite.Apply(gameObject, enemyData != null ? enemyData.MapSprite : null);
    }

    // The one on the map first, then its group: rolled at the first battle, the same one after.
    private List<EnemyData> GetEnemies()
    {
        if (!EncounterGroups.TryGet(encounterId, out int[] extraEnemies))
        {
            extraEnemies = RollExtraEnemies();
            EncounterGroups.Remember(encounterId, extraEnemies);
        }

        var enemies = new List<EnemyData> { enemyData };
        foreach (int extraEnemy in extraEnemies)
            enemies.Add(GetExtraEnemy(extraEnemy));

        return enemies;
    }

    private int[] RollExtraEnemies()
    {
        var candidates = new List<int>();
        for (int i = 0; i < possibleExtraEnemies.Count; i++)
        {
            if (possibleExtraEnemies[i] != null)
                candidates.Add(i);
        }

        int extraCount = Random.Range(minExtraEnemies, Mathf.Max(minExtraEnemies, maxExtraEnemies) + 1);
        var extraEnemies = new int[extraCount];
        for (int i = 0; i < extraCount; i++)
            extraEnemies[i] = candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : SameEnemy;

        return extraEnemies;
    }

    // One no longer in the list (it was edited after the group was rolled): the enemy itself.
    private EnemyData GetExtraEnemy(int index)
    {
        return index >= 0 && index < possibleExtraEnemies.Count && possibleExtraEnemies[index] != null
            ? possibleExtraEnemies[index]
            : enemyData;
    }

    // Back on the map the player stands a step away from it, so the battle does not start again at
    // once. A wall stops the step short: with a cliff at his back he stays at the cliff, not in it.
    private Vector3 GetPlayerReturnPosition(Collider2D player)
    {
        Vector3 position = player.transform.position;
        Vector2 direction = position - transform.position;
        direction = direction.sqrMagnitude <= 0.0001f ? Vector2.down : direction.normalized;

        Collider2D feet = FeetOf(player);
        float step = feet != null ? FreeDistance(feet, direction, playerReturnOffset) : 0f;
        return position + (Vector3)(direction * step);
    }

    private static Collider2D FeetOf(Collider2D part)
    {
        if (!part.isTrigger)
            return part;

        foreach (Collider2D sibling in part.GetComponents<Collider2D>())
        {
            if (!sibling.isTrigger)
                return sibling;
        }

        return null;
    }

    // How far the feet can go that way before a wall. People (enemies, companions) do not count:
    // they all stand elsewhere once the map loads again.
    private static float FreeDistance(Collider2D feet, Vector2 direction, float distance)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(feet.gameObject.layer));

        float free = distance;
        int count = feet.Cast(direction, filter, ReturnHits, distance + WallGap);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = ReturnHits[i];
            Rigidbody2D hitBody = hit.collider != null ? hit.collider.attachedRigidbody : null;
            if (hit.collider == null || (hitBody != null && hitBody.bodyType != RigidbodyType2D.Static))
                continue;

            // A wall he is leaving behind.
            if (Vector2.Dot(hit.normal, direction) > 0f)
                continue;

            free = Mathf.Min(free, hit.distance - WallGap);
        }

        return Mathf.Max(0f, free);
    }
}
