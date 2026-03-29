using System.Collections.Generic;
using System.Linq;

public static class InventorySelectionController
{
    public static List<CardData> GetSortedUniqueCards(
        IReadOnlyList<CardData> cards)
    {
        if (cards == null)
            return new List<CardData>();

        return cards
            .GroupBy(c => c)
            .Select(g => g.Key)
            .OrderBy(c => c.CardName)
            .ToList();
    }

    public static CardData GetByIndex(
        IReadOnlyList<CardData> cards,
        int index)
    {
        var sorted = GetSortedUniqueCards(cards);

        if (index < 0 || index >= sorted.Count)
            return null;

        return sorted[index];
    }
}