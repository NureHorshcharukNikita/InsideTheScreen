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
    private readonly Action<CardData, InventoryCardLocation, int, Action<int>> requestTransferQuantity;

    private CardCategory? currentCategory;
    private List<CardData> filteredCards;

    private DeckData deckData;

    private CardData selectedCard;
    private bool selectedFromDeck;
    private TMP_Text inventoryCountText;
    private TMP_Text deckCountText;

    public InventoryScreenController(
        InventoryData storage,
        DeckData deck,
        InventoryPreviewPanel preview,
        CardCollectionDrawer inventoryDrawer,
        CardCollectionDrawer deckDrawer,
        Action<CardData, InventoryCardLocation, int, Action<int>> requestTransferQuantity = null)
    {
        inventoryStorage = storage;
        deckData = deck;
        inventoryPreviewPanel = preview;
        this.inventoryDrawer = inventoryDrawer;
        this.deckDrawer = deckDrawer;
        this.requestTransferQuantity = requestTransferQuantity;

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
        inventoryCountText = inventoryText;
        deckCountText = deckText;

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
        if (!GameStateManager.CanEditCardCollections())
            return;

        var card = selectionPresenter.GetSelected();

        if (TryAddToDeck(card, 1))
            RefreshAfterCardCollectionChanged();
    }

    public void RemoveSelected()
    {
        if (!GameStateManager.CanEditCardCollections())
            return;

        var card = selectionPresenter.GetSelected();

        if (TryRemoveFromDeck(card, 1))
            RefreshAfterCardCollectionChanged();
    }

    private bool TryAddToDeck(CardData card, int quantity)
    {
        if (card == null)
            return false;

        if (!deckController.CanAdd(card))
            return false;

        return deckController.Add(card, quantity) > 0;
    }

    private bool TryRemoveFromDeck(CardData card, int quantity)
    {
        if (card == null || !deckController.IsInDeck(card))
            return false;

        return deckController.Remove(card, quantity) > 0;
    }

    private void RequestQuantityOrTransfer(
        CardData card,
        InventoryCardLocation source,
        int maxQuantity,
        Action<int> transfer)
    {
        if (card == null || transfer == null || maxQuantity <= 0)
            return;

        if (maxQuantity > 1 && requestTransferQuantity != null)
        {
            requestTransferQuantity(card, source, maxQuantity, quantity =>
            {
                int clampedQuantity = System.Math.Clamp(quantity, 1, maxQuantity);
                transfer(clampedQuantity);
            });

            return;
        }

        transfer(1);
    }

    private void RefreshAfterCardCollectionChanged()
    {
        selectedCard = null;
        selectedFromDeck = false;
        inventoryPreviewPanel.Clear();
        RefreshViewOnly();
        RefreshCounters();
    }

    private void RefreshCounters()
    {
        InventoryCounters.Update(
            inventoryCountText,
            deckCountText,
            inventoryStorage,
            deckData);
    }

}
