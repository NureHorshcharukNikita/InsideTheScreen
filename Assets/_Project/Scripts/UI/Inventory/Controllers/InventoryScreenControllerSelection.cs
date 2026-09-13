public partial class InventoryScreenController
{
    private void OnInventoryCardClicked(CardData card)
    {
        if (card == null)
            return;

        selectedCard = card;
        selectedFromDeck = false;

        selectionPresenter.ShowFromInventory(card);
        RefreshViewOnly();
    }

    private void OnDeckCardClicked(CardData card)
    {
        if (!deckController.HasDeck())
            return;

        if (card == null)
            return;

        selectedCard = card;
        selectedFromDeck = true;

        selectionPresenter.ShowFromDeck(card);
        RefreshViewOnly();
    }

    private void OnInventoryCardDropped(
        CardData card,
        InventoryCardLocation source,
        InventoryCardLocation destination)
    {
        if (!GameStateManager.CanEditCardCollections())
            return;

        if (card == null || source == destination)
            return;

        if (source == InventoryCardLocation.Inventory && destination == InventoryCardLocation.Deck)
        {
            RequestQuantityOrTransfer(
                card,
                source,
                deckController.GetAddableCount(card),
                quantity =>
                {
                    if (TryAddToDeck(card, quantity))
                        RefreshAfterCardCollectionChanged();
                });

            return;
        }

        if (source == InventoryCardLocation.Deck && destination == InventoryCardLocation.Inventory)
        {
            RequestQuantityOrTransfer(
                card,
                source,
                deckController.GetDeckCount(card),
                quantity =>
                {
                    if (TryRemoveFromDeck(card, quantity))
                        RefreshAfterCardCollectionChanged();
                });
        }
    }

    public void SetCategory(CardCategory? category)
    {
        currentCategory = category;

        if (selectedCard == null)
            return;

        if (selectedFromDeck)
            return;

        if (!CardCategoryUtils.HasCategory(selectedCard, currentCategory))
            ClearSelection();
    }

    public void ClearSelection()
    {
        selectedCard = null;
        selectedFromDeck = false;
        inventoryPreviewPanel.Clear();
    }

    private void ValidateSelection()
    {
        if (selectedCard == null)
            return;

        if (selectedFromDeck)
        {
            if (!deckController.IsInDeck(selectedCard))
                ClearSelection();

            return;
        }

        bool visibleInInventory = filteredCards == null || filteredCards.Contains(selectedCard);

        if (!visibleInInventory)
            ClearSelection();
    }
}
