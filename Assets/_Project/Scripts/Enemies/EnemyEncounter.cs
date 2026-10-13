using System.Collections.Generic;
using UnityEngine;

// An enemy on the map: walking into it starts a battle against it, and against whoever is with it:
// a few more enemies may join, rolled at the first battle and the same ones every time after
// (EncounterGroups). The map only ever shows this one. Companions following the player fight at
// the player's side.
public class EnemyEncounter : MonoBehaviour
{
    // In a saved group: one more of the enemy itself, not one of Possible Extra Enemies.
    public const int SameEnemy = -1;

    private const int MaxExtraEnemies = PendingBattleEnemy.MaxPerSide - 1;

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
    [SerializeField] private float playerReturnOffset = 1.25f;

    private static int nextEncounterId = 1;

    private bool isTriggered = false;

    public static void ResetEncounterIds()
    {
        nextEncounterId = 1;
    }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(encounterId))
            encounterId = (nextEncounterId++).ToString();
    }

    private void Start()
    {
        if (DefeatedEncounters.IsDefeated(encounterId))
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            isTriggered = true;
            ExplorationPlayerSession.SavePosition(GetPlayerReturnPosition(other.transform));
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

    private Vector3 GetPlayerReturnPosition(Transform player)
    {
        Vector3 direction = player.position - transform.position;
        direction.z = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = Vector3.down;

        return player.position + direction.normalized * playerReturnOffset;
    }
}
