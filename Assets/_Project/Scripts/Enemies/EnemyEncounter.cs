using UnityEngine;

public class EnemyEncounter : MonoBehaviour
{
    [SerializeField] private GameObject battleEnemyPrefab;
    [SerializeField] private float playerReturnOffset = 1.25f;

    private static int nextEncounterId = 1;

    private string encounterId;
    private bool isTriggered = false;

    public static void ResetEncounterIds()
    {
        nextEncounterId = 1;
    }

    private void Awake()
    {
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

            PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                playerMovement.StopMovement();
                playerMovement.enabled = false;
            }

            PendingBattleEnemy.RegisterEncounterStart(battleEnemyPrefab, encounterId);
            FadeManager.TryFadeToScene(SceneNames.Battle);
        }
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
