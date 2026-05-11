using System.Linq;

public partial class InventoryScreenController
{
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

    private void RefreshViewOnly()
    {
        DrawInventory();

        if (deckController.HasDeck())
            DrawDeck();
    }
}
