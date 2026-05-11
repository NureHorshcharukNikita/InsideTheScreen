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
        if (card == null || deck == null || storage == null)
            return false;

        int inventoryCopies = storage.Cards.Count(storedCard => storedCard == card);
        int deckCopies = deck.Cards.Count(deckCard => deckCard == card);

        return deckCopies < inventoryCopies && deck.Cards.Count < deck.MaxCount;
    }

    public void Add(CardData card)
    {
        if (card == null || deck == null)
            return;

        if (!CanAdd(card))
            return;

        deck.AddCard(card);
    }

    public void Remove(CardData card)
    {
        if (card == null || deck == null)
            return;

        deck.RemoveCard(card);
    }

    public bool IsInDeck(CardData card)
    {
        return deck != null && deck.Cards.Contains(card);
    }
}
