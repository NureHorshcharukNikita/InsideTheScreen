using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CardCollectionDrawer
{
    private readonly Transform content;
    private readonly CardView cardPrefab;

    private readonly List<CardView> pool = new();

    public CardCollectionDrawer(Transform content, CardView cardPrefab)
    {
        this.content = content;
        this.cardPrefab = cardPrefab;
    }

    public void Draw(IReadOnlyList<CardData> cards, Action<int> onClick)
    {
        Clear();

        if (cards == null || content == null || cardPrefab == null)
            return;

        Dictionary<CardData, int> counts = cards
            .GroupBy(c => c)
            .ToDictionary(g => g.Key, g => g.Count());

        List<CardData> sortedCards = cards
            .GroupBy(c => c)
            .Select(g => g.Key)
            .OrderBy(c => c.CardName)
            .ToList();

        for (int i = 0; i < sortedCards.Count; i++)
        {
            var card = sortedCards[i];

            CardView cardView = GetPool();
            cardView.Setup(card, counts[card], i, onClick);
        }

        UpdateContentHeight(sortedCards.Count);
    }

    public void Clear()
    {
        for (int i = 0; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }

    private CardView GetPool()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeSelf)
            {
                pool[i].gameObject.SetActive(true);
                return pool[i];
            }
        }

        var newCard = UnityEngine.Object.Instantiate(cardPrefab, content);
        pool.Add(newCard);
        return newCard;
    }

    private void UpdateContentHeight(int itemCount)
    {
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        RectTransform rt = content.GetComponent<RectTransform>();

        int columns = grid.constraintCount;
        int rows = Mathf.CeilToInt((float)itemCount / columns);

        float height =
            grid.padding.top +
            grid.padding.bottom +
            rows * grid.cellSize.y +
            (rows - 1) * grid.spacing.y;

        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }
}