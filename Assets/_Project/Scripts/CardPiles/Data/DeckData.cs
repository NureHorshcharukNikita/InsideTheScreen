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
        if (card == null)
            return;

        if (cards.Count >= maxCount)
            return;

        cards.Add(card);
        ExplorationPlayerSession.SavePersistent();
    }

    public void RemoveCard(CardData card)
    {
        if (card == null)
            return;

        cards.Remove(card);
        ExplorationPlayerSession.SavePersistent();
    }

    public void Clear()
    {
        cards.Clear();
        ExplorationPlayerSession.SavePersistent();
    }

    public void ReplaceCards(IEnumerable<CardData> newCards)
    {
        cards = newCards != null
            ? new List<CardData>(newCards)
            : new List<CardData>();
    }
}
