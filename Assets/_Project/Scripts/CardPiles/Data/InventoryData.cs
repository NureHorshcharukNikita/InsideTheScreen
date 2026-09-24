using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Inventory Data")]
public class InventoryData : ScriptableObject
{
    [SerializeField] private List<CardData> cards = new();

    public IReadOnlyList<CardData> Cards => cards;

    public InventoryData CreateRuntimeCopy()
    {
        InventoryData copy = CreateInstance<InventoryData>();
        copy.name = $"{name}_Runtime";
        copy.cards = new List<CardData>(cards);
        return copy;
    }

    public void AddCard(CardData card)
    {
        AddCards(card, 1);
    }

    public int AddCards(CardData card, int count)
    {
        if (card == null || count <= 0)
            return 0;

        for (int i = 0; i < count; i++)
            cards.Add(card);

        return count;
    }

    public void RemoveCard(CardData card)
    {
        RemoveCards(card, 1);
    }

    public int RemoveCards(CardData card, int count)
    {
        if (card == null || count <= 0)
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
