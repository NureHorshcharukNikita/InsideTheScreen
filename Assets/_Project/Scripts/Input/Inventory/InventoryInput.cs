using UnityEngine;

public class InventoryInput : MonoBehaviour
{
    [SerializeField] private InventoryWindow inventoryWindow;

    private void Update()
    {
        if (inventoryWindow == null)
            return;

        // Handled here because InventoryWindow is inactive while hidden.
        if (GameInputSettings.GetKeyDown(GameInputAction.Close))
        {
            if (inventoryWindow.IsOpen || inventoryWindow.IsPauseShown)
                inventoryWindow.HandleCloseInput();
            else if (GameStateManager.CanPause())
                inventoryWindow.PauseGame();

            return;
        }

        if (!GameInputSettings.GetKeyDown(GameInputAction.Inventory))
            return;

        if (inventoryWindow.IsOpen || GameStateManager.CanOpenInventory())
            inventoryWindow.Toggle();
    }

    // Switching to another window in the middle of play opens the pause menu. Not during a story
    // event or a dialogue: those wait where they are (GameClock) and go on when the game is back.
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && inventoryWindow != null && GameStateManager.CanPause())
            inventoryWindow.PauseGame();
    }
}
