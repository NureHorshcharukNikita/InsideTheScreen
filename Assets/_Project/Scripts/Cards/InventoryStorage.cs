using System.Collections.Generic;
using UnityEngine;

public class InventoryStorage : MonoBehaviour
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