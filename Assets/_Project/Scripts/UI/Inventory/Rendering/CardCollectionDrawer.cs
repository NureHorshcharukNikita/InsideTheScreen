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

    public void Draw(
        IReadOnlyList<CardData> cards,
        Action<int> onClick,
        Func<CardData, bool> isSelected = null)
    {
        Clear();

        if (cards == null || content == null || cardPrefab == null)
            return;

        Dictionary<CardData, int> counts = cards
            .GroupBy(card => card)
            .ToDictionary(group => group.Key, group => group.Count());

        List<CardData> sortedCards = cards
            .GroupBy(card => card)
            .Select(group => group.Key)
            .OrderBy(card => card.CardName)
            .ToList();

        for (int i = 0; i < sortedCards.Count; i++)
        {
            var card = sortedCards[i];
            var cardView = GetPool();

            bool selected = isSelected != null && isSelected(card);

            cardView.Setup(card, counts[card], i, onClick, null, selected);
        }

        UpdateContentHeight(sortedCards.Count);
    }

    public void Clear()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            pool[i].SetSelected(false);
            pool[i].gameObject.SetActive(false);
        }
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
        RectTransform rectTransform = content.GetComponent<RectTransform>();

        int columns = grid.constraintCount;
        int rows = Mathf.CeilToInt((float)itemCount / columns);

        float height =
            grid.padding.top +
            grid.padding.bottom +
            rows * grid.cellSize.y +
            (rows - 1) * grid.spacing.y;

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
    }
}
