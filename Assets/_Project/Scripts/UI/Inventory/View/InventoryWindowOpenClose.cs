public partial class InventoryWindow
{
    public void Open()
    {
        GameStateManager.SetState(GameState.Inventory);
        gameObject.SetActive(true);

        inventoryScreenController.ClearSelection();

        inventoryPreviewPanel.Clear();
        SelectTopIcon(0);
        Refresh();
    }

    public void Close()
    {
        inventoryScreenController.ClearSelection();

        GameStateManager.SetState(GameState.Gameplay);
        gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }
}
