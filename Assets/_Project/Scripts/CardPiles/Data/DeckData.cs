using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Deck Data")]
public class DeckData : ScriptableObject
{
    [SerializeField] private int maxCount = 16;
    [SerializeField] private List<CardData> cards = new();

    public int MaxCount => maxCount;
    public IReadOnlyList<CardData> Cards => cards;

    public void AddCard(CardData card)
    {
        if (card == null)
            return;

        if (cards.Count >= maxCount)
            return;

        cards.Add(card);
    }

    public void RemoveCard(CardData card)
    {
        if (card == null)
            return;

        cards.Remove(card);
    }

    public void Clear()
    {
        cards.Clear();
    }
}