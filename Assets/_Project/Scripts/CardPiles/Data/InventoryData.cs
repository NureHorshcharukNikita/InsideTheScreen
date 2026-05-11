using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Inventory Data")]
public class InventoryData : ScriptableObject
{
    [SerializeField] private List<CardData> cards = new();

    public IReadOnlyList<CardData> Cards => cards;

    public void AddCard(CardData card)
    {
        if (card == null) return;
        cards.Add(card);
    }

    public void RemoveCard(CardData card)
    {
        if (card == null) return;
        cards.Remove(card);
    }

    public void Clear()
    {
        cards.Clear();
    }
}