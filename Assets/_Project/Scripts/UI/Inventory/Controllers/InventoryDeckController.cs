using System.Linq;

public class InventoryDeckController
{
    private readonly InventoryData storage;
    private readonly DeckData deck;

    public InventoryDeckController(InventoryData storage, DeckData deck)
    {
        this.storage = storage;
        this.deck = deck;
    }

    public bool HasDeck()
    {
        return deck != null;
    }

    public bool CanAdd(CardData card)
    {
        return GetAddableCount(card) > 0;
    }

    public int GetAddableCount(CardData card)
    {
        if (card == null || deck == null || storage == null)
            return 0;

        int inventoryCopies = storage.Cards.Count(storedCard => storedCard == card);
        int deckCopies = GetDeckCount(card);
        int freeDeckSlots = deck.MaxCount - deck.Cards.Count;

        return System.Math.Max(0, System.Math.Min(inventoryCopies - deckCopies, freeDeckSlots));
    }

    public int GetDeckCount(CardData card)
    {
        if (card == null || deck == null)
            return 0;

        return deck.Cards.Count(deckCard => deckCard == card);
    }

    public void Add(CardData card)
    {
        Add(card, 1);
    }

    public int Add(CardData card, int count)
    {
        if (card == null || deck == null)
            return 0;

        int quantity = System.Math.Min(count, GetAddableCount(card));
        if (quantity <= 0)
            return 0;

        return deck.AddCards(card, quantity);
    }

    public void Remove(CardData card)
    {
        Remove(card, 1);
    }

    public int Remove(CardData card, int count)
    {
        if (card == null || deck == null)
            return 0;

        int quantity = System.Math.Min(count, GetDeckCount(card));
        if (quantity <= 0)
            return 0;

        return deck.RemoveCards(card, quantity);
    }

    public bool IsInDeck(CardData card)
    {
        return deck != null && deck.Cards.Contains(card);
    }
}
