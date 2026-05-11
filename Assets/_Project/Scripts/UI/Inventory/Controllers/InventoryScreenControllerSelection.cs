using System;

public partial class InventoryScreenController
{
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

    public void SetCategory(Type category)
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
