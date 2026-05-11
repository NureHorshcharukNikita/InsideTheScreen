using UnityEngine;
using UnityEngine.UI;

public static partial class HandDrawDeckFlyAnimationDotween
{
    private static bool TryPrepare(HandDrawDeckFlyContext context, int drawCount, out HandDeckFlyPreparedData preparedFlyData)
    {
        int handCount = context.SpawnedViews.Count;
        int actualDrawCount = Mathf.Min(drawCount, handCount);
        int firstDrawIndex = handCount - actualDrawCount;
        if (firstDrawIndex < 0 || handCount != context.DeckManager.Hand.Count)
        {
            preparedFlyData = default;
            return false;
        }

        context.FlyingBuffer.Clear();
        var targetsWorld = new Vector3[actualDrawCount];
        var layoutElements = new LayoutElement[actualDrawCount];
        var views = new CardView[actualDrawCount];

        Vector3 stackStartWorld = ResolveStackStartWorld(context);

        for (int index = 0; index < actualDrawCount; index++)
            PrepareFlyingCard(context, firstDrawIndex + index, index, stackStartWorld, targetsWorld, layoutElements, views);

        preparedFlyData = new HandDeckFlyPreparedData(firstDrawIndex, actualDrawCount, stackStartWorld, targetsWorld, layoutElements, views);
        return true;
    }

    private static void PrepareFlyingCard(
        HandDrawDeckFlyContext context,
        int sourceIndex,
        int targetIndex,
        Vector3 stackStartWorld,
        Vector3[] targetsWorld,
        LayoutElement[] layoutElements,
        CardView[] views)
    {
        CardView cardView = context.SpawnedViews[sourceIndex];
        var cardRect = cardView.transform as RectTransform;
        views[targetIndex] = cardView;
        targetsWorld[targetIndex] = cardRect.position;
        layoutElements[targetIndex] = cardView.GetComponent<LayoutElement>();
        if (layoutElements[targetIndex] != null)
            layoutElements[targetIndex].ignoreLayout = true;

        cardRect.SetParent(context.CanvasRoot, worldPositionStays: false);
        cardRect.SetAsLastSibling();
        cardRect.position = stackStartWorld;
        HandCardFlyTween.SetCardFlyVisible(cardRect, false);
        context.FlyingBuffer.Add(cardRect);
    }
}
