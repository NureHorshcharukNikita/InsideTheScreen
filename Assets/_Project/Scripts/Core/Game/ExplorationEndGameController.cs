using System.Collections;
using TMPro;
using UnityEngine;

// Shows the end-of-game panel once the victory condition is met: a story flag set by the final
// fight (the Occult Guardian sets occultist_defeated when he falls).
public sealed class ExplorationEndGameController : MonoBehaviour
{
    [Tooltip("The game is won once this story flag is set.")]
    [SerializeField, StoryFlag] private string victoryStoryFlag = "occultist_defeated";
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private string title = "You saved the town!";
    [SerializeField] private string message = "The Occult Guardian has fallen. The town is safe.";
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
        if (endGameShown || string.IsNullOrWhiteSpace(victoryStoryFlag) || !StoryFlagManager.HasFlag(victoryStoryFlag))
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
