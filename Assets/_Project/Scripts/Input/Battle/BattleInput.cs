using UnityEngine;

public class BattleInput : MonoBehaviour
{
    [SerializeField] private BattleSystem battleSystem;

    private void Update()
    {
        if (!GameStateManager.IsGameplay)
            return;

        if (battleSystem == null)
            return;

        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        if (battleSystem.IsTurnTransitionInProgress)
            battleSystem.RequestSkipTurnTransition();
        else if (battleSystem.CanPlay())
            battleSystem.EndTurn();
    }
}
