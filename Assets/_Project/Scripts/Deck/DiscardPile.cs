using System.Collections.Generic;

public class DiscardPile
{
    private List<CardData> cards = new();

    public int Count => cards.Count;
    public IReadOnlyList<CardData> Cards => cards;

    public void Add(CardData card)
    {
        if (card == null)
            return;

        cards.Add(card);
    }

    public void Clear()
    {
        cards.Clear();
    }
}