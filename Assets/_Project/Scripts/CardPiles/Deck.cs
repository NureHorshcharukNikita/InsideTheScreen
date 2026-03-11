using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    private List<CardData> cards = new();

    public int Count => cards.Count;
    public IReadOnlyList<CardData> Cards => cards;

    private int lastIndex => Count - 1;

    public void Add(CardData card)
    {
        if (card == null)
            return;

        cards.Add(card);
    }

    public CardData Draw()
    {
        if (cards.Count == 0)
            return null;

        var card = cards[lastIndex];
        cards.RemoveAt(lastIndex);

        return card;
    }

    public void Shuffle()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            int randomIndex = Random.Range(i, cards.Count);

            (cards[i], cards[randomIndex]) = (cards[randomIndex], cards[i]);
        }
    }
}
