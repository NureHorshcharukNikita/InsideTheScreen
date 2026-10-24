using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// A stretch of forest road where enemies now and then spring out of the bushes as the player walks
// it (its trigger colliders mark the stretch). Every Check Every units walked inside, a roll against
// Chance, if the last ambush anywhere was long enough ago (Balance Config). Then a "!" pops over
// Haru, and the leader of a group comes out a few steps away and runs at him like any enemy on the
// map (EnemyChaser): outrun it, or fight. How many come grows with how strong Haru is
// (AmbushDifficulty); who comes is picked from the zone's ambushers Haru is strong enough for.
// Ambushers that lose the player go back into the bushes, and a beaten one does not come back.
public sealed class AmbushZone : MonoBehaviour
{
    [Serializable]
    private sealed class Ambusher
    {
        [Tooltip("The enemy as it looks on the map, e.g. Enemy_Wolf.")]
        public EnemyEncounter prefab;
        [Tooltip("Comes only once Haru's power (AmbushDifficulty.HeroPower) is at least this.")]
        [Min(0f)] public float minPower;
        [Tooltip("Higher = likelier.")]
        [Min(1)] public int weight = 1;
    }

    private const string IdPrefix = "ambush_";
    private const float EmergeSeconds = 0.35f;
    private const float PlayerAlertSeconds = 0.6f;
    // Ambushers this far from the player go back into the bushes.
    private const float LoseDistance = 16f;
    private const float ClearRadius = 0.6f;

    // WorldClock seconds from which the next ambush may come, on any road.
    private static float nextAmbushAt = -1f;
    private static bool cleanedUp;
    private static readonly List<GameObject> Active = new();

    [SerializeField] private List<Ambusher> ambushers = new();
    [Tooltip("All of them must be met, e.g. only once Haru has left Leymar.")]
    [SerializeField] private List<StoryCondition> conditions = new();
    [Tooltip("A roll every this many units walked inside the zone.")]
    [SerializeField, Min(1f)] private float checkEvery = 10f;
    [SerializeField, Range(0f, 1f)] private float chance = 0.35f;
    [Tooltip("How far from the player they come out.")]
    [SerializeField, Min(2f)] private float spawnDistance = 3.5f;

    private Transform player;
    private int playerInside;
    private Vector2 lastPosition;
    private float walked;

    private void Awake()
    {
        // Ambushes come and go: beaten ones from earlier visits are not kept.
        if (!cleanedUp)
        {
            cleanedUp = true;
            DefeatedEncounters.ForgetAll(IdPrefix);
            EncounterGroups.ForgetAll(IdPrefix);
        }

        // Not the moment the game starts or loads.
        if (nextAmbushAt < 0f)
            nextAmbushAt = WorldClock.PlaySeconds + Balance.Config.secondsBetweenAmbushes * 0.5f;
    }

    private void OnDestroy()
    {
        cleanedUp = false;
        Active.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        player = other.transform;
        if (playerInside++ == 0)
            lastPosition = player.position;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = Mathf.Max(0, playerInside - 1);
    }

    private void Update()
    {
        Active.RemoveAll(ambusher => ambusher == null);
        if (playerInside == 0 || player == null || !GameStateManager.IsGameplay)
            return;

        Vector2 position = player.position;
        walked += Vector2.Distance(position, lastPosition);
        lastPosition = position;
        if (walked < checkEvery)
            return;

        walked = 0f;
        if (Active.Count == 0 && WorldClock.PlaySeconds >= nextAmbushAt && StoryCondition.AreMet(conditions)
            && Random.value < chance)
            Spring();
    }

    private void Spring()
    {
        float power = AmbushDifficulty.HeroPower;
        List<Ambusher> candidates = ambushers.FindAll(option => option?.prefab != null && option.minPower <= power);
        if (candidates.Count == 0)
        {
            Ambusher weakest = null;
            foreach (Ambusher option in ambushers)
            {
                if (option?.prefab != null && (weakest == null || option.minPower < weakest.minPower))
                    weakest = option;
            }

            if (weakest == null)
                return;

            candidates.Add(weakest);
        }

        if (!TryFindSpot(out Vector2 spot))
            return;

        nextAmbushAt = WorldClock.PlaySeconds + Balance.Config.secondsBetweenAmbushes;

        EnemyEncounter leader = Pick(candidates).prefab;
        var group = new List<EnemyData>();
        for (int i = 1; i < AmbushDifficulty.NextGroupSize(); i++)
            group.Add(Pick(candidates).prefab.Data);

        EnemyEncounter ambusher = Instantiate(leader, spot, Quaternion.identity);
        ambusher.name = $"Ambush_{leader.name}";
        ambusher.SetUpAmbush(IdPrefix + Guid.NewGuid().ToString("N").Substring(0, 8), group);
        Active.Add(ambusher.gameObject);
        StartCoroutine(Emerge(ambusher.gameObject));
    }

    private static Ambusher Pick(List<Ambusher> candidates)
    {
        int total = 0;
        foreach (Ambusher option in candidates)
            total += Mathf.Max(1, option.weight);

        int roll = Random.Range(0, total);
        foreach (Ambusher option in candidates)
        {
            roll -= Mathf.Max(1, option.weight);
            if (roll < 0)
                return option;
        }

        return candidates[0];
    }

    // Out of the way of walls, trees and water, a few steps away, with nothing between it and Haru.
    private bool TryFindSpot(out Vector2 spot)
    {
        Vector2 from = player.position;
        float startAngle = Random.Range(0f, 360f);
        for (int i = 0; i < 12; i++)
        {
            float angle = (startAngle + i * 30f) * Mathf.Deg2Rad;
            spot = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnDistance;
            if (IsClear(spot) && !IsBlocked(from, spot))
                return true;
        }

        spot = default;
        return false;
    }

    private static bool IsClear(Vector2 spot)
    {
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(spot, ClearRadius))
        {
            if (!hit.isTrigger)
                return false;
        }

        return true;
    }

    private static bool IsBlocked(Vector2 from, Vector2 to)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(from, to))
        {
            if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
                return true;
        }

        return false;
    }

    // It fades in out of the bushes as a "!" pops over Haru; then it lives like any enemy on the map,
    // until it loses the player.
    private IEnumerator Emerge(GameObject ambusher)
    {
        GameObject alert = AlertMark.Create(player);
        SpriteRenderer sprite = ambusher.GetComponent<SpriteRenderer>();
        Color shown = sprite != null ? sprite.color : Color.white;
        for (float elapsed = 0f; elapsed < EmergeSeconds && ambusher != null; elapsed += Time.deltaTime)
        {
            if (sprite != null)
                sprite.color = new Color(shown.r, shown.g, shown.b, shown.a * elapsed / EmergeSeconds);
            yield return null;
        }

        if (sprite != null)
            sprite.color = shown;

        yield return new WaitForSeconds(Mathf.Max(0f, PlayerAlertSeconds - EmergeSeconds));
        if (alert != null)
            Destroy(alert);

        while (ambusher != null)
        {
            if (player == null || Vector2.Distance(player.position, ambusher.transform.position) > LoseDistance)
            {
                Destroy(ambusher);
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
}
