using System.Collections.Generic;

public class DeckManager
{
    public Deck Deck { get; private set; }
    public Hand Hand { get; } = new();
    public DiscardPile DiscardPile { get; } = new();

    public void Initialize(IReadOnlyCollection<CardData> startingDeck)
    {
        if (startingDeck == null)
        {
            Deck = new Deck(0);
            return;
        }

        Deck = new Deck(startingDeck.Count);

        foreach (var card in startingDeck)
            Deck.Add(card);

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
            {
                Hand.Add(card);
                DevLog.Log("Draw card: " + card.CardName);
                DevLog.Log("Deck Count: " + Deck.Count);
            }
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

    private void ReshuffleDiscard()
    {
        if (DiscardPile.Count == 0)
            return;

        DevLog.Log("Reshuffling discard pile");

        foreach (var card in DiscardPile.Cards)
            Deck.Add(card);

        DiscardPile.Clear();
        Deck.Shuffle();

        BattleDebugPrinter.PrintCards("Deck order", Deck.Cards);
    }
}
