using System.Collections;
using DG.Tweening;
using UnityEngine;

public static partial class HandDrawDeckFlyAnimationDotween
{
    private static IEnumerator AnimateCardsToHand(HandDrawDeckFlyContext context, HandDeckFlyPreparedData preparedFlyData)
    {
        float duration = Mathf.Max(1e-4f, context.Settings.Duration);
        Tween activeTween = null;

        try
        {
            for (int index = 0; index < preparedFlyData.DrawCount; index++)
            {
                if (index > 0)
                    yield return new WaitForSeconds(context.Settings.Stagger);

                if (index >= context.FlyingBuffer.Count)
                    yield break;

                RectTransform flyingCard = context.FlyingBuffer[index];
                if (flyingCard == null)
                    continue;

                flyingCard.position = preparedFlyData.StackStartWorld;
                HandCardFlyTween.SetCardFlyVisible(flyingCard, true);
                context.OnDeckFlyCardArrived?.Invoke();

                if (index >= preparedFlyData.TargetsWorld.Length)
                    yield break;

                Vector3 start = flyingCard.position;
                Vector3 end = preparedFlyData.TargetsWorld[index];
                Vector3 sceneSplineStart = default;
                Vector3 sceneSplineEnd = default;
                bool hasSceneSpline = TryGetSceneSplineAnchors(context, out sceneSplineStart, out sceneSplineEnd);

                activeTween = DOVirtual.Float(0f, 1f, duration, progress =>
                {
                    if (flyingCard == null)
                        return;

                    float easedProgress = DOVirtual.EasedValue(0f, 1f, progress, Ease.OutCubic);
                    flyingCard.position = EvaluateFlightPosition(
                        context,
                        hasSceneSpline,
                        start,
                        end,
                        sceneSplineStart,
                        sceneSplineEnd,
                        easedProgress);
                }).SetEase(Ease.Linear);

                yield return activeTween.WaitForCompletion();

                if (flyingCard != null)
                    flyingCard.position = end;

                activeTween = null;
            }
        }
        finally
        {
            activeTween?.Kill();
        }
    }

    private static void RestoreCardsToHand(HandDrawDeckFlyContext context, HandDeckFlyPreparedData preparedFlyData)
    {
        for (int index = 0; index < preparedFlyData.DrawCount; index++)
        {
            if (index >= preparedFlyData.Views.Length || index >= preparedFlyData.LayoutElements.Length)
                break;

            CardView cardView = preparedFlyData.Views[index];
            if (cardView == null)
                continue;

            var cardRect = cardView.transform as RectTransform;
            cardRect.SetParent(context.HandPanel, worldPositionStays: true);
            cardRect.SetSiblingIndex(preparedFlyData.FirstDrawIndex + index);

            if (preparedFlyData.LayoutElements[index] != null)
                preparedFlyData.LayoutElements[index].ignoreLayout = false;

            HandCardFlyTween.SetCardFlyVisible(cardRect, true);
            cardView.RefreshDragHierarchyAfterReparent();
        }
    }
}
