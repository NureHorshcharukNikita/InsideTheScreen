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

        if (!GameInputSettings.GetKeyDown(GameInputAction.EndTurn))
            return;

        if (battleSystem.IsTurnTransitionInProgress)
            battleSystem.RequestSkipTurnTransition();
        else if (battleSystem.CanPlay())
            battleSystem.EndTurn();
    }
}
