using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;
using Unity.Mathematics;

public static class HandDrawDeckFlyAnimationDotween
{
    private readonly struct PreparedFlyData
    {
        public readonly int FirstDrawIndex;
        public readonly int DrawCount;
        public readonly Vector3 StackStartWorld;
        public readonly Vector3[] TargetsWorld;
        public readonly LayoutElement[] LayoutElements;
        public readonly CardView[] Views;

        public PreparedFlyData(
            int firstDrawIndex,
            int drawCount,
            Vector3 stackStartWorld,
            Vector3[] targetsWorld,
            LayoutElement[] layoutElements,
            CardView[] views)
        {
            FirstDrawIndex = firstDrawIndex;
            DrawCount = drawCount;
            StackStartWorld = stackStartWorld;
            TargetsWorld = targetsWorld;
            LayoutElements = layoutElements;
            Views = views;
        }
    }

    public static IEnumerator Run(HandDrawDeckFlyContext context, int drawCount)
    {
        if (!TryPrepare(context, drawCount, out PreparedFlyData preparedFlyData))
        {
            HandCardSequence.CopySnapshot(context.DeckManager.Hand.Cards, context.SnapshotDestination);
            yield break;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(context.HandRect);
        if (context.Settings.DealStartDelay > 0f)
            yield return new WaitForSeconds(context.Settings.DealStartDelay);
        else
            yield return null;

        yield return AnimateCardsToHand(context, preparedFlyData);

        RestoreCardsToHand(context, preparedFlyData);
        context.FlyingBuffer.Clear();
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.HandRect);
        HandCardSequence.CopySnapshot(context.DeckManager.Hand.Cards, context.SnapshotDestination);
    }

    private static bool TryPrepare(HandDrawDeckFlyContext context, int drawCount, out PreparedFlyData preparedFlyData)
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

        preparedFlyData = new PreparedFlyData(firstDrawIndex, actualDrawCount, stackStartWorld, targetsWorld, layoutElements, views);
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

    private static IEnumerator AnimateCardsToHand(HandDrawDeckFlyContext context, PreparedFlyData preparedFlyData)
    {
        float duration = Mathf.Max(1e-4f, context.Settings.Duration);
        Tween activeTween = null;

        try
        {
            for (int index = 0; index < preparedFlyData.DrawCount; index++)
            {
                if (index > 0)
                    yield return new WaitForSeconds(context.Settings.Stagger);

                RectTransform flyingCard = context.FlyingBuffer[index];
                if (flyingCard == null)
                    continue;

                flyingCard.position = preparedFlyData.StackStartWorld;
                HandCardFlyTween.SetCardFlyVisible(flyingCard, true);
                context.OnDeckFlyCardArrived?.Invoke();

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

    private static void RestoreCardsToHand(HandDrawDeckFlyContext context, PreparedFlyData preparedFlyData)
    {
        for (int index = 0; index < preparedFlyData.DrawCount; index++)
        {
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

    private static bool TryGetSceneSplineAnchors(HandDrawDeckFlyContext context, out Vector3 sceneStart, out Vector3 sceneEnd)
    {
        sceneStart = default;
        sceneEnd = default;
        if (!TryEvaluateSceneSplineWorld(context, 0f, out sceneStart))
            return false;
        if (!TryEvaluateSceneSplineWorld(context, 1f, out sceneEnd))
            return false;
        return true;
    }

    private static bool TryEvaluateSceneSplineWorld(HandDrawDeckFlyContext context, float t, out Vector3 worldPoint)
    {
        worldPoint = default;
        if (context.FlightSpline == null || context.FlightSpline.Spline == null)
            return false;

        float3 localPoint = context.FlightSpline.Spline.EvaluatePosition(Mathf.Clamp01(t));
        worldPoint = context.FlightSpline.transform.TransformPoint(new Vector3(localPoint.x, localPoint.y, localPoint.z));
        return true;
    }

    private static Vector3 ResolveStackStartWorld(HandDrawDeckFlyContext context)
    {
        if (TryEvaluateSceneSplineWorld(context, 0f, out Vector3 splineStart))
            return splineStart;

        return context.HandPanel.position;
    }

    private static Vector3 EvaluateFlightPosition(
        HandDrawDeckFlyContext context,
        bool hasSceneSpline,
        Vector3 start,
        Vector3 end,
        Vector3 sceneSplineStart,
        Vector3 sceneSplineEnd,
        float progress)
    {
        if (hasSceneSpline && TryEvaluateSceneSplineWorld(context, progress, out Vector3 splineWorldPoint))
        {
            Vector3 offsetFromStart = start - sceneSplineStart;
            Vector3 anchoredPoint = splineWorldPoint + offsetFromStart;
            Vector3 endCorrection = end - (sceneSplineEnd + offsetFromStart);
            return anchoredPoint + endCorrection * progress;
        }

        return Vector3.Lerp(start, end, progress);
    }
}
