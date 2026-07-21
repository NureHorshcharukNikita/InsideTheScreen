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
        if (card == null) return;
        cards.Add(card);
        ExplorationPlayerSession.SavePersistent();
    }

    public void RemoveCard(CardData card)
    {
        if (card == null) return;
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
