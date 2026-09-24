using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Deck Data")]
public class DeckData : ScriptableObject
{
    [SerializeField] private int maxCount = 16;
    [SerializeField] private List<CardData> cards = new();

    public int MaxCount => maxCount;
    public IReadOnlyList<CardData> Cards => cards;

    public DeckData CreateRuntimeCopy()
    {
        DeckData copy = CreateInstance<DeckData>();
        copy.name = $"{name}_Runtime";
        copy.maxCount = maxCount;
        copy.cards = new List<CardData>(cards);
        return copy;
    }

    public void AddCard(CardData card)
    {
        AddCards(card, 1);
    }

    public int AddCards(CardData card, int count)
    {
        if (card == null)
            return 0;

        int added = 0;
        while (added < count && cards.Count < maxCount)
        {
            cards.Add(card);
            added++;
        }

        return added;
    }

    public void RemoveCard(CardData card)
    {
        RemoveCards(card, 1);
    }

    public int RemoveCards(CardData card, int count)
    {
        if (card == null)
            return 0;

        int removed = 0;
        while (removed < count && cards.Remove(card))
            removed++;

        return removed;
    }

    public void Clear()
    {
        cards.Clear();
    }

    public void ReplaceCards(IEnumerable<CardData> newCards)
    {
        cards = newCards != null
            ? new List<CardData>(newCards)
            : new List<CardData>();
    }
}
