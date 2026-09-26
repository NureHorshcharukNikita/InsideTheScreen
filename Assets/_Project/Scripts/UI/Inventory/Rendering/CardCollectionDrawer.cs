using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryCardLocation
{
    None,
    Inventory,
    Deck
}

public class CardCollectionDrawer
{
    private readonly Transform content;
    private readonly CardView cardPrefab;

    private readonly List<CardView> pool = new();

    public RectTransform DropArea { get; }

    public CardCollectionDrawer(Transform content, CardView cardPrefab)
    {
        this.content = content;
        this.cardPrefab = cardPrefab;
        DropArea = ResolveDropArea(content);
    }

    public void Draw(
        IReadOnlyList<CardData> cards,
        Action<CardData> onClick,
        Func<CardData, bool> isSelected = null,
        InventoryCardLocation source = InventoryCardLocation.None,
        RectTransform inventoryDropArea = null,
        RectTransform deckDropArea = null,
        Action<CardData, InventoryCardLocation, InventoryCardLocation> onDrop = null)
    {
        if (cards == null || content == null || cardPrefab == null)
        {
            DeactivateUnused(0);
            return;
        }

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
            var cardView = GetPool(i);

            bool selected = isSelected != null && isSelected(card);

            cardView.Setup(card, counts[card], i, _ => onClick?.Invoke(card), null, selected);
            cardView.ConfigureInventoryTransfer(
                card,
                source,
                inventoryDropArea,
                deckDropArea,
                onDrop);
        }

        DeactivateUnused(sortedCards.Count);

        UpdateContentHeight(sortedCards.Count);
    }

    public void Clear()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            pool[i].ClearInventoryTransfer();
            pool[i].SetSelected(false);
            pool[i].gameObject.SetActive(false);
        }
    }

    private CardView GetPool(int index)
    {
        if (index < pool.Count)
        {
            CardView cardView = pool[index];
            if (!cardView.gameObject.activeSelf)
                cardView.gameObject.SetActive(true);

            return cardView;
        }

        var newCard = UnityEngine.Object.Instantiate(cardPrefab, content);
        pool.Add(newCard);
        return newCard;
    }

    private void DeactivateUnused(int firstUnusedIndex)
    {
        for (int i = firstUnusedIndex; i < pool.Count; i++)
        {
            CardView cardView = pool[i];
            if (!cardView.gameObject.activeSelf)
                continue;

            cardView.ClearInventoryTransfer();
            cardView.SetSelected(false);
            cardView.gameObject.SetActive(false);
        }
    }

    private static RectTransform ResolveDropArea(Transform content)
    {
        if (content == null)
            return null;

        ScrollRect scrollRect = content.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            return scrollRect.transform.parent as RectTransform
                ?? scrollRect.transform as RectTransform;

        return content as RectTransform;
    }

    private void UpdateContentHeight(int itemCount)
    {
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        RectTransform rectTransform = content.GetComponent<RectTransform>();

        if (grid == null || rectTransform == null)
            return;

        int columns = Mathf.Max(1, grid.constraintCount);
        int rows = itemCount > 0
            ? Mathf.CeilToInt((float)itemCount / columns)
            : 0;

        float height =
            grid.padding.top +
            grid.padding.bottom +
            rows * grid.cellSize.y +
            Mathf.Max(0, rows - 1) * grid.spacing.y;

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
    }
}
