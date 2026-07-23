using System.Collections;
using TMPro;
using UnityEngine;

public sealed class ExplorationEndGameController : MonoBehaviour
{
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private string title = "You saved the town!";
    [SerializeField] private string message = "All monsters have been defeated.";
    [SerializeField] private bool clearSaveOnReturnToMenu = true;

    private bool endGameShown;

    private IEnumerator Start()
    {
        SetEndGamePanelVisible(false);

        yield return null;

        TryShowEndGame();
    }

    public void ReturnToMainMenu()
    {
        if (clearSaveOnReturnToMenu)
        {
            ExplorationPlayerSession.Clear();
            PendingBattleEnemy.ClearSession();
            DefeatedEncounters.Clear();
            EnemyEncounter.ResetEncounterIds();
        }

        MainMenuNavigation.GoToMainMenu();
    }

    private void TryShowEndGame()
    {
        if (endGameShown || !DefeatedEncounters.HasAny())
            return;

        EnemyEncounter[] remainingEnemies = FindObjectsByType<EnemyEncounter>(FindObjectsSortMode.None);
        if (remainingEnemies.Length > 0)
            return;

        endGameShown = true;

        if (titleText != null)
            titleText.text = title;
        if (messageText != null)
            messageText.text = message;

        GameStateManager.SetState(GameState.Inventory);
        SetEndGamePanelVisible(true);
    }

    private void SetEndGamePanelVisible(bool visible)
    {
        if (endGamePanel != null)
            endGamePanel.SetActive(visible);
    }
}
