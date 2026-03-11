using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    private List<CardData> cards = new();

    public int Count => cards.Count;

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

        var card = cards[0];
        cards.RemoveAt(0);
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
