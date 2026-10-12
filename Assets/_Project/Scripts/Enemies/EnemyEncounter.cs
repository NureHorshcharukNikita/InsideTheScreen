using System.Collections.Generic;
using UnityEngine;

// An enemy on the map: walking into it starts a battle against it, and against whoever happens to
// be with it this time: a few more enemies may join, rolled anew for every battle. The map only
// ever shows this one. Companions following the player fight at the player's side.
public class EnemyEncounter : MonoBehaviour
{
    private const int MaxExtraEnemies = PendingBattleEnemy.MaxPerSide - 1;

    [SerializeField] private EnemyData enemyData;

    [Header("Group")]
    [Tooltip("How many more enemies join it at least, rolled for every battle.")]
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

            PendingBattleEnemy.RegisterEncounterStart(RollEnemies(), CompanionFollower.CollectBattleAllies(), encounterId);
            FadeManager.TryFadeToScene(SceneNames.Battle);
        }
    }

    private void OnValidate()
    {
        maxExtraEnemies = Mathf.Max(maxExtraEnemies, minExtraEnemies);
    }

    // The one on the map first, then whoever joins it this time.
    private List<EnemyData> RollEnemies()
    {
        var enemies = new List<EnemyData> { enemyData };
        int extraCount = Random.Range(minExtraEnemies, Mathf.Max(minExtraEnemies, maxExtraEnemies) + 1);
        for (int i = 0; i < extraCount; i++)
            enemies.Add(PickExtraEnemy());

        return enemies;
    }

    private EnemyData PickExtraEnemy()
    {
        List<EnemyData> candidates = possibleExtraEnemies.FindAll(candidate => candidate != null);
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : enemyData;
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
