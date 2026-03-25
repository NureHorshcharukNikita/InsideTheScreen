using System;
using System.Collections.Generic;

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

    public void Add(CardData card)
    {
        if (card == null)
            return;

        if (Count >= MaxCount)
            return;

        cards.Add(card);

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
