using TMPro;

public class InventoryScreenController
{
    private readonly InventoryData inventoryStorage;
    private readonly InventoryPreviewPanel inventoryPreviewPanel;

    private readonly CardCollectionDrawer inventoryDrawer;
    private readonly CardCollectionDrawer deckDrawer;

    private readonly InventoryDeckController deckController;
    private readonly InventoryPreviewController previewController;
    private readonly InventorySelectionPresenter selectionPresenter;

    private DeckData deckData;

    public InventoryScreenController(
        InventoryData storage,
        DeckData deck,
        InventoryPreviewPanel preview,
        CardCollectionDrawer inventoryDrawer,
        CardCollectionDrawer deckDrawer)
    {
        inventoryStorage = storage;
        deckData = deck;
        inventoryPreviewPanel = preview;
        this.inventoryDrawer = inventoryDrawer;
        this.deckDrawer = deckDrawer;

        deckController = new InventoryDeckController(inventoryStorage, deckData);

        previewController = new InventoryPreviewController(
            inventoryPreviewPanel,
            inventoryStorage,
            deckController);

        selectionPresenter = new InventorySelectionPresenter(
            inventoryPreviewPanel,
            deckController);
    }

    public void Refresh(TMP_Text inventoryText, TMP_Text deckText)
    {
        inventoryDrawer.Clear();
        deckDrawer.Clear();

        if (inventoryStorage == null)
        {
            InventoryCounters.SetEmpty(inventoryText, deckText);
            return;
        }

        DrawInventory();

        if (deckController.HasDeck())
            DrawDeck();

        InventoryCounters.Update(
            inventoryText,
            deckText,
            inventoryStorage,
            deckData);
    }

    private void DrawInventory()
    {
        inventoryDrawer.Draw(
            inventoryStorage.Cards,
            OnInventoryCardClicked);
    }

    private void DrawDeck()
    {
        deckDrawer.Draw(
            deckData.Cards,
            OnDeckCardClicked);
    }

    private void OnInventoryCardClicked(int index)
    {
        var card = InventorySelectionController
            .GetByIndex(inventoryStorage.Cards, index);

        selectionPresenter.ShowFromInventory(card);
    }

    private void OnDeckCardClicked(int index)
    {
        if (!deckController.HasDeck())
            return;

        var card = InventorySelectionController
            .GetByIndex(
                deckData.Cards,
                index);

        selectionPresenter.ShowFromDeck(card);
    }

    public void AddSelected()
    {
        var card = selectionPresenter.GetSelected();

        if (card == null)
            return;

        if (!deckController.CanAdd(card))
            return;

        deckController.Add(card);
        previewController.Update();
    }

    public void RemoveSelected()
    {
        var card = selectionPresenter.GetSelected();

        if (card == null)
            return;

        deckController.Remove(card);
        previewController.Update();
    }
}