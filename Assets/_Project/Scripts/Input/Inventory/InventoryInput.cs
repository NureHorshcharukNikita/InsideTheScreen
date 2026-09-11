using UnityEngine;

public class InventoryInput : MonoBehaviour
{
    [SerializeField] private InventoryWindow inventoryWindow;

    private void Update()
    {
        if (inventoryWindow == null)
            return;

        if (!GameInputSettings.GetKeyDown(GameInputAction.Inventory))
            return;

        if (inventoryWindow.IsOpen || GameStateManager.CanOpenInventory())
            inventoryWindow.Toggle();
    }
}
