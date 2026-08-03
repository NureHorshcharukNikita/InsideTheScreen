using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SessionCardSaveMapper
{
    public static string[] ToIds(IReadOnlyList<CardData> cards)
    {
        if (cards == null)
            return null;

        return cards
            .Where(card => card != null && !string.IsNullOrEmpty(card.CardID))
            .Select(card => card.CardID)
            .ToArray();
    }

    public static IEnumerable<CardData> ResolveCards(IEnumerable<string> cardIds, IReadOnlyDictionary<string, CardData> cardsById)
    {
        if (cardIds == null)
            yield break;

        foreach (string cardId in cardIds)
        {
            if (!string.IsNullOrEmpty(cardId) && cardsById.TryGetValue(cardId, out CardData card))
                yield return card;
        }
    }

    public static Dictionary<string, CardData> BuildLookup(params ScriptableObject[] sources)
    {
        Dictionary<string, CardData> cardsById = new();

        foreach (ScriptableObject source in sources)
        {
            IEnumerable<CardData> cards = source switch
            {
                InventoryData inventory => inventory.Cards,
                DeckData deck => deck.Cards,
                _ => null
            };

            if (cards == null)
                continue;

            foreach (CardData card in cards)
            {
                if (card != null && !string.IsNullOrEmpty(card.CardID))
                    cardsById.TryAdd(card.CardID, card);
            }
        }

        return cardsById;
    }
}
