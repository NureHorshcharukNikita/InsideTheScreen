using UnityEngine;

public class EnemyEncounter : MonoBehaviour
{
    private bool isTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;

        if (other.CompareTag("Player"))
        {
            isTriggered = true;

            PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                playerMovement.StopMovement();
                playerMovement.enabled = false;
            }

            FadeManager.Instance.FadeToScene(SceneNames.Battle);
        }
    }
}