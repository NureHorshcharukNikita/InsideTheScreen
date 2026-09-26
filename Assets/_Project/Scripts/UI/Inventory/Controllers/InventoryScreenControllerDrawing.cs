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

        if (!string.IsNullOrEmpty(currentSearchQuery))
        {
            filteredCards = filteredCards
                .Where(CardNameMatchesSearch)
                .ToList();
        }
    }

    public void SetSearchQuery(string query)
    {
        string normalizedQuery = query?.Trim() ?? string.Empty;
        if (string.Equals(currentSearchQuery, normalizedQuery, System.StringComparison.Ordinal))
            return;

        currentSearchQuery = normalizedQuery;

        if (inventoryStorage == null)
            return;

        BuildFilteredCards();
        ValidateSelection();
        DrawInventory();
    }

    private bool CardNameMatchesSearch(CardData card)
    {
        if (card == null)
            return false;

        string cardName = string.IsNullOrWhiteSpace(card.CardName)
            ? card.name
            : card.CardName;

        return cardName.IndexOf(currentSearchQuery, System.StringComparison.OrdinalIgnoreCase) >= 0;
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
