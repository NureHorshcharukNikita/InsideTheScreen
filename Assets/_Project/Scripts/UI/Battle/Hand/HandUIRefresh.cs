using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class HandUI
{
    private void RefreshHand(DeckManager deckManager, int? selectedCardIndex)
    {
        if (battleSystem == null)
            return;

        _lastDeckManager = deckManager;
        _lastSelectedCardIndex = selectedCardIndex;

        if (deckManager == null || deckManager.Hand == null)
        {
            ClearRenderedHand();
            return;
        }

        if (deckManager.Hand.Count == 0)
        {
            ClearRenderedHand();
            return;
        }

        IReadOnlyList<CardData> newHand = deckManager.Hand.Cards;
        if (_flyRoutine != null && IsSameHandSnapshot(newHand))
            return;

        StopFlyRoutineIfAny();
        HandCardViewCollection.DestroyAllSpawnedCardViews(handPanel, _spawnedViews);

        bool suffixDraw = HandCardSequence.TryComputeSuffixDraw(_lastHandOrder, newHand, out int drawCount);

        HandCardViewCollection.CreateCardViews(handPanel, _spawnedViews, deckManager, cardPrefab, battleSystem);

        var handRect = handPanel as RectTransform;
        Canvas canvas = handRect != null ? handRect.GetComponentInParent<Canvas>() : null;
        RectTransform canvasRoot = canvas != null ? canvas.transform as RectTransform : null;

        bool shouldFly = animateDeckToHand
            && suffixDraw
            && drawCount > 0
            && handRect != null
            && canvas != null
            && canvasRoot != null;

        if (shouldFly)
            HandCardViewCollection.HideNewestCards(_spawnedViews, drawCount);

        if (handRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(handRect);
        }

        if (shouldFly)
        {
            HandCardSequence.CopySnapshot(newHand, _lastHandOrder);

            var settings = new HandFlyTweenSettings(
                flyDealStartDelay,
                flyDuration,
                flyStagger);

            _flyRoutine = StartCoroutine(FlyThenClearRoutine(drawCount, deckManager, handRect, canvasRoot, settings));
        }
        else
        {
            HandCardSequence.CopySnapshot(newHand, _lastHandOrder);
            DrawFlyAnimationCompleted?.Invoke();
        }
    }

    private void ClearRenderedHand()
    {
        StopFlyRoutineIfAny();
        HandCardViewCollection.DestroyAllSpawnedCardViews(handPanel, _spawnedViews);
        _lastHandOrder.Clear();
    }

    private bool IsSameHandSnapshot(IReadOnlyList<CardData> hand)
    {
        if (hand == null || hand.Count != _lastHandOrder.Count)
            return false;

        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] != _lastHandOrder[i])
                return false;
        }

        return true;
    }

    private IEnumerator FlyThenClearRoutine(
        int drawCount,
        DeckManager deckManager,
        RectTransform handRect,
        RectTransform canvasRoot,
        HandFlyTweenSettings settings)
    {
        deckUi?.BeginDeckCountFlyStagger(drawCount);

        Action onCardArrived = deckUi != null ? deckUi.OnDeckFlyCardArrived : null;
        var context = new HandDrawDeckFlyContext(
            handPanel,
            _spawnedViews,
            _currentlyFlying,
            _lastHandOrder,
            deckManager,
            handRect,
            canvasRoot,
            handFlightSpline,
            settings,
            onCardArrived);

        bool completedCleanly = false;
        try
        {
            IEnumerator inner = HandDrawDeckFlyAnimationDotween.Run(context, drawCount);

            while (inner.MoveNext())
                yield return inner.Current;

            completedCleanly = true;
        }
        finally
        {
            deckUi?.EndDeckCountFlyStagger();
            _flyRoutine = null;
            if (completedCleanly)
                DrawFlyAnimationCompleted?.Invoke();
        }
    }
}
