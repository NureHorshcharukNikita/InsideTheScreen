using System.Linq;

public partial class InventoryScreenController
{
    private void BuildFilteredCards()
    {
        filteredCards = inventoryStorage.Cards.ToList();

        if (currentCategory != null)
        {
            filteredCards = filteredCards
                .Where(card => CardCategoryUtils.HasCategory(card, currentCategory))
                .ToList();
        }
    }

    private void DrawInventory()
    {
        if (filteredCards == null)
            BuildFilteredCards();

        inventoryDrawer.Draw(
            filteredCards,
            OnInventoryCardClicked,
            card => !selectedFromDeck && card == selectedCard,
            InventoryCardLocation.Inventory,
            inventoryDrawer.DropArea,
            deckDrawer.DropArea,
            OnInventoryCardDropped);

        setInventoryEmptyStateVisible?.Invoke(filteredCards.Count == 0);
    }

    private void DrawDeck()
    {
        bool deckIsEmpty = deckData.Cards.Count == 0;

        deckDrawer.Draw(
            deckData.Cards,
            OnDeckCardClicked,
            card => selectedFromDeck && card == selectedCard,
            InventoryCardLocation.Deck,
            inventoryDrawer.DropArea,
            deckDrawer.DropArea,
            OnInventoryCardDropped);

        setDeckEmptyStateVisible?.Invoke(deckIsEmpty);
    }

    private void RefreshViewOnly()
    {
        BuildFilteredCards();
        ValidateSelection();

        DrawInventory();

        if (deckController.HasDeck())
            DrawDeck();
        else
            setDeckEmptyStateVisible?.Invoke(false);
    }
}
