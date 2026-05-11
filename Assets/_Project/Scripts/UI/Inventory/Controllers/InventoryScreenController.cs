using System;
using System.Collections.Generic;
using TMPro;

public partial class InventoryScreenController
{
    private readonly InventoryData inventoryStorage;
    private readonly InventoryPreviewPanel inventoryPreviewPanel;

    private readonly CardCollectionDrawer inventoryDrawer;
    private readonly CardCollectionDrawer deckDrawer;

    private readonly InventoryDeckController deckController;
    private readonly InventoryPreviewController previewController;
    private readonly InventorySelectionPresenter selectionPresenter;

    private Type currentCategory;
    private List<CardData> filteredCards;

    private DeckData deckData;

    private CardData selectedCard;
    private bool selectedFromDeck;

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

        ValidateSelection();

        DrawInventory();

        if (deckController.HasDeck())
            DrawDeck();

        InventoryCounters.Update(
            inventoryText,
            deckText,
            inventoryStorage,
            deckData);
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

        if (selectedFromDeck && !deckController.IsInDeck(card))
        {
            selectedCard = null;
            selectedFromDeck = false;
            inventoryPreviewPanel.Clear();
        }
        else
        {
            previewController.Update();
        }
    }

}
