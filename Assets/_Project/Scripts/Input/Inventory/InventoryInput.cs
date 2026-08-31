using UnityEngine;

public class InventoryInput : MonoBehaviour
{
    [SerializeField] private InventoryWindow inventoryWindow;

    private void Update()
    {
        if (!GameStateManager.CanOpenInventory())
            return;

        if (GameInputSettings.GetKeyDown(GameInputAction.Inventory))
            inventoryWindow.Toggle();
    }
}
