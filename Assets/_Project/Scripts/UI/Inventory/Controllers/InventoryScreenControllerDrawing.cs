using System.Linq;

public partial class InventoryScreenController
{
    private void DrawInventory()
    {
        filteredCards = inventoryStorage.Cards.ToList();

        if (currentCategory != null)
        {
            filteredCards = filteredCards
                .Where(card => CardCategoryUtils.HasCategory(card, currentCategory))
                .ToList();
        }

        inventoryDrawer.Draw(
            filteredCards,
            OnInventoryCardClicked,
            card => !selectedFromDeck && card == selectedCard,
            InventoryCardLocation.Inventory,
            inventoryDrawer.DropArea,
            deckDrawer.DropArea,
            OnInventoryCardDropped);
    }

    private void DrawDeck()
    {
        deckDrawer.Draw(
            deckData.Cards,
            OnDeckCardClicked,
            card => selectedFromDeck && card == selectedCard,
            InventoryCardLocation.Deck,
            inventoryDrawer.DropArea,
            deckDrawer.DropArea,
            OnInventoryCardDropped);
    }

    private void RefreshViewOnly()
    {
        DrawInventory();

        if (deckController.HasDeck())
            DrawDeck();
    }
}
