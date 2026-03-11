using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class DeckManager
{
    public Deck Deck { get; } = new();
    public Hand Hand { get; } = new();
    public DiscardPile DiscardPile { get; } = new();

    public void Initialize(List<CardData> startingDeck)
    {
        foreach (var card in startingDeck)
            Deck.Add(card);

        Deck.Shuffle();
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (Deck.Count == 0 && Hand.Count == 0)
                ReshuffleDiscard();

            var card = Deck.Draw();

            if (card != null)
            {
                Hand.Add(card);
                Debug.Log("Draw card: " + card.CardName);
                Debug.Log("Deck Count: " + Deck.Count);
            }
        }
    }

    public void Discard(CardData card)
    {
        Hand.Remove(card);
        DiscardPile.Add(card);
    }

    private void ReshuffleDiscard()
    {
        if (DiscardPile.Count == 0)
            return;

        Debug.Log("Reshuffling discard pile");

        foreach (var card in DiscardPile.Cards)
            Deck.Add(card);

        DiscardPile.Clear();
        Deck.Shuffle();

        BattleDebugPrinter.PrintCards("Deck order", Deck.Cards);
    }
}