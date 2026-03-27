using System;
using System.Collections.Generic;
using System.Linq;
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

    private void DrawInventory()
    {
        filteredCards = inventoryStorage.Cards.ToList();

        if (currentCategory != null)
        {
            filteredCards = filteredCards
                .Where(c => CardCategoryUtils.HasCategory(c, currentCategory))
                .ToList();
        }

        inventoryDrawer.Draw(
            filteredCards,
            OnInventoryCardClicked,
            card => !selectedFromDeck && card == selectedCard);
    }

    private void DrawDeck()
    {
        deckDrawer.Draw(
            deckData.Cards,
            OnDeckCardClicked,
            card => selectedFromDeck && card == selectedCard);
    }

    private void OnInventoryCardClicked(int index)
    {
        var card = InventorySelectionController.GetByIndex(filteredCards, index);

        selectedCard = card;
        selectedFromDeck = false;

        selectionPresenter.ShowFromInventory(card);
        RefreshViewOnly();
    }

    private void OnDeckCardClicked(int index)
    {
        if (!deckController.HasDeck())
            return;

        var card = InventorySelectionController.GetByIndex(deckData.Cards, index);

        selectedCard = card;
        selectedFromDeck = true;

        selectionPresenter.ShowFromDeck(card);
        RefreshViewOnly();
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

    public void SetCategory(Type category)
    {
        currentCategory = category;

        if (selectedCard == null)
            return;

        if (selectedFromDeck)
            return;

        if (!CardCategoryUtils.HasCategory(selectedCard, currentCategory))
        {
            selectedCard = null;
            inventoryPreviewPanel.Clear();
        }
    }

    private void RefreshViewOnly()
    {
        DrawInventory();

        if (deckController.HasDeck())
            DrawDeck();
    }

    private void ValidateSelection()
    {
        if (selectedCard == null)
            return;

        if (selectedFromDeck)
        {
            if (!deckController.IsInDeck(selectedCard))
            {
                selectedCard = null;
                selectedFromDeck = false;
                inventoryPreviewPanel.Clear();
            }

            return;
        }

        bool visibleInInventory = filteredCards == null || filteredCards.Contains(selectedCard);

        if (!visibleInInventory)
        {
            selectedCard = null;
            selectedFromDeck = false;
            inventoryPreviewPanel.Clear();
        }
    }
}