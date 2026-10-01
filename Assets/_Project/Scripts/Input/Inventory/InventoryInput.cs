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
}
