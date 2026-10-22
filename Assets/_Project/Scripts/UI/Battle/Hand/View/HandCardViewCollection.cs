using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class HandCardViewCollection
{
    public static void DestroyAllSpawnedCardViews(Transform handPanel, List<CardView> spawnedViews)
    {
        for (int i = spawnedViews.Count - 1; i >= 0; i--)
        {
            CardView cardView = spawnedViews[i];
            if (cardView != null)
                DestroyCardView(cardView);
        }

        spawnedViews.Clear();

        for (int i = handPanel.childCount - 1; i >= 0; i--)
        {
            CardView cardView = handPanel.GetChild(i).GetComponent<CardView>();
            if (cardView != null)
                DestroyCardView(cardView);
        }
    }

    // Destroy only takes effect at the end of the frame, but the new hand is laid out in this same
    // frame: hidden right away, the old card no longer takes a place in it.
    private static void DestroyCardView(CardView cardView)
    {
        cardView.gameObject.SetActive(false);
        Object.Destroy(cardView.gameObject);
    }

    // While cards fly in from the deck the hand's layout holds still: the cards already in hand
    // keep their places instead of closing up, and each flying card lands on the place left for it.
    public static void SetLayoutFrozen(Transform handPanel, bool frozen)
    {
        if (handPanel != null && handPanel.TryGetComponent(out LayoutGroup layout))
            layout.enabled = !frozen;
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
            // Cards are played by dragging them onto a fighter; a click does nothing.
            cardView.Setup(cardData, index, onSelect: null, battleSystem, selected: false);
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

            if (flyingCard.TryGetComponent(out LayoutElement layoutElement))
                layoutElement.ignoreLayout = false;

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
