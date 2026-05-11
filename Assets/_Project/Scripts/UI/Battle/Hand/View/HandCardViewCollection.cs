using System.Collections.Generic;
using UnityEngine;

public static class HandCardViewCollection
{
    public static void DestroyAllSpawnedCardViews(Transform handPanel, List<CardView> spawnedViews)
    {
        for (int i = spawnedViews.Count - 1; i >= 0; i--)
        {
            CardView cardView = spawnedViews[i];
            if (cardView != null)
                Object.Destroy(cardView.gameObject);
        }

        spawnedViews.Clear();

        for (int i = handPanel.childCount - 1; i >= 0; i--)
        {
            CardView cardView = handPanel.GetChild(i).GetComponent<CardView>();
            if (cardView != null)
                Object.Destroy(cardView.gameObject);
        }
    }

    public static void CreateCardViews(
        Transform handPanel,
        List<CardView> spawnedViews,
        DeckManager deckManager,
        CardView cardPrefab,
        BattleSystem battleSystem)
    {
        if (cardPrefab == null || battleSystem == null)
            return;

        for (int i = 0; i < deckManager.Hand.Count; i++)
        {
            CardData cardData = deckManager.Hand.Cards[i];
            int index = i;

            CardView cardView = Object.Instantiate(cardPrefab, handPanel);
            cardView.Setup(cardData, index, battleSystem.SelectCard, battleSystem, selected: false);
            spawnedViews.Add(cardView);
        }
    }

    public static void HideNewestCards(List<CardView> spawnedViews, int drawCount)
    {
        int handCount = spawnedViews.Count;
        int hideFrom = Mathf.Max(0, handCount - drawCount);
        for (int i = hideFrom; i < handCount; i++)
        {
            CardView cardView = spawnedViews[i];
            if (cardView != null)
                HandCardFlyTween.SetCardFlyVisible(cardView.transform, false);
        }
    }

    public static void SnapFlyingCardsToHand(Transform handPanel, List<RectTransform> currentlyFlying)
    {
        for (int i = 0; i < currentlyFlying.Count; i++)
        {
            RectTransform flyingCard = currentlyFlying[i];
            if (flyingCard == null)
                continue;

            if (flyingCard.parent != handPanel)
                flyingCard.SetParent(handPanel, worldPositionStays: true);

            HandCardFlyTween.SetCardFlyVisible(flyingCard, true);
        }
    }

    public static void RefreshDragHierarchyForChildren(Transform handPanel)
    {
        for (int i = 0; i < handPanel.childCount; i++)
        {
            CardView cardView = handPanel.GetChild(i).GetComponent<CardView>();
            if (cardView != null)
                cardView.RefreshDragHierarchyAfterReparent();
        }
    }
}
