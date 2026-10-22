using System;
using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    private List<CardData> cards = new();

    public int MaxCount { get; private set; }
    public int Count => cards.Count;
    public IReadOnlyList<CardData> Cards => cards;

    private int lastIndex => Count - 1;

    public event Action<int, int> DeckCountChanged;

    public Deck(int maxCount)
    {
        MaxCount = maxCount;
    }

    public Deck(DeckData deckData)
    {
        if (deckData == null)
        {
            MaxCount = 0;
            return;
        }

        cards = new List<CardData>(deckData.Cards);
        MaxCount = cards.Count;
    }

    public void Add(CardData card)
    {
        if (card == null)
            return;

        if (Count >= MaxCount)
            return;

        cards.Add(card);

        NotifyDeckChanged();
    }

    // A card new to the battle, e.g. a base card topping up a short deck: it goes in at a random
    // place and counts towards the deck's size from now on.
    public void Insert(CardData card)
    {
        if (card == null)
            return;

        MaxCount++;
        cards.Insert(UnityEngine.Random.Range(0, cards.Count + 1), card);

        NotifyDeckChanged();
    }

    // A card that left the battle for good (broken, used up) no longer counts towards the deck's
    // size, wherever it was (hand or discard pile).
    public void Shrink()
    {
        if (MaxCount == 0)
            return;

        MaxCount--;

        NotifyDeckChanged();
    }

    public void Remove(CardData card)
    {
        if (card == null)
            return;

        if (cards.Remove(card))
            NotifyDeckChanged();
    }

    public CardData Draw()
    {
        if (cards.Count == 0)
            return null;

        var card = cards[lastIndex];
        cards.RemoveAt(lastIndex);

        NotifyDeckChanged();

        return card;
    }

    public void Shuffle()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, cards.Count);
            (cards[i], cards[randomIndex]) = (cards[randomIndex], cards[i]);
        }
    }

    private void NotifyDeckChanged()
    {
        DeckCountChanged?.Invoke(Count, MaxCount);
    }
}
