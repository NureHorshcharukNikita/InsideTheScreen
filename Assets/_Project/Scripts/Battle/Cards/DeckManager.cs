using System.Collections.Generic;

public class DeckManager
{
    public Deck Deck { get; private set; }
    public Hand Hand { get; } = new();
    public DiscardPile DiscardPile { get; } = new();

    // Every card of the battle, wherever it is now.
    public int CardsInBattle => Deck.Count + Hand.Count + DiscardPile.Count;

    private IReadOnlyList<CardData> baseCards = new List<CardData>();
    private int baseCardsAdded;
    private int minimumSize;

    // baseCards in the order they top up a deck smaller than minimumSize (PlayerData).
    public void Initialize(DeckData deckData, IReadOnlyList<CardData> baseCards, int minimumSize)
    {
        Deck = new Deck(deckData);
        this.baseCards = baseCards ?? new List<CardData>();
        this.minimumSize = minimumSize;
        baseCardsAdded = 0;

        TopUpWithBaseCards();
        Deck.Shuffle();
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (Deck.Count == 0)
                ReshuffleDiscard();

            var card = Deck.Draw();

            if (card != null)
                Hand.Add(card);
        }
    }

    public void DiscardByCardFromHand(CardData card)
    {
        Hand.Remove(card);
        DiscardPile.Add(card);
    }

    public void DiscardByIndexFromHand(int index)
    {
        if (index < 0 || index >= Hand.Count)
            return;

        var card = Hand.Cards[index];

        Hand.RemoveAt(index);
        DiscardPile.Add(card);
    }

    // For a card that breaks or is used up when played: it leaves the battle instead of going to
    // the discard pile, and base cards take its place while the deck is short of the minimum.
    public void RemoveFromBattleByIndexFromHand(int index)
    {
        if (index < 0 || index >= Hand.Count)
            return;

        Hand.RemoveAt(index);
        Deck.Shrink();
        TopUpWithBaseCards();
    }

    // Base cards come in their fixed order and only so many of them, so they can run out too.
    private void TopUpWithBaseCards()
    {
        while (CardsInBattle < minimumSize && baseCardsAdded < baseCards.Count)
        {
            Deck.Insert(baseCards[baseCardsAdded]);
            baseCardsAdded++;
        }
    }

    private void ReshuffleDiscard()
    {
        if (DiscardPile.Count == 0)
            return;

        foreach (var card in DiscardPile.Cards)
            Deck.Add(card);

        DiscardPile.Clear();
        Deck.Shuffle();
    }
}
