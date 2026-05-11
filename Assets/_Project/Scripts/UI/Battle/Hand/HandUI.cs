using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

public class HandUI : MonoBehaviour
{
    public event Action DrawFlyAnimationCompleted;

    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private DeckUI deckUi;

    [Header("Draw from deck (fly-in)")]
    [SerializeField] private bool animateDeckToHand = true;
    [SerializeField] private float flyDealStartDelay = 0.12f;
    [SerializeField] private float flyDuration = 0.35f;
    [SerializeField] private float flyStagger = 0.08f;
    [SerializeField] private SplineContainer handFlightSpline;
    [SerializeField] private bool autoCreateSplineIfMissing = true;

    private Transform handPanel;
    private readonly List<CardData> _lastHandOrder = new();
    private readonly List<CardView> _spawnedViews = new();
    private readonly List<RectTransform> _currentlyFlying = new();
    private Coroutine _flyRoutine;
    private bool _wasGameplayState = true;
    private DeckManager _lastDeckManager;
    private int? _lastSelectedCardIndex;

    private void Awake()
    {
        handPanel = transform;
        _wasGameplayState = GameStateManager.IsGameplay;

        if (deckUi == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.rootCanvas != null)
                deckUi = canvas.rootCanvas.GetComponentInChildren<DeckUI>(true);
        }

        handFlightSpline = HandSplineSetup.EnsureFlightSplineExists(transform, deckUi, handFlightSpline, autoCreateSplineIfMissing);
    }

    [ContextMenu("Create Hand Flight Spline")]
    private void CreateHandFlightSplineContext()
    {
        handFlightSpline = HandSplineSetup.CreateDefaultFlightSpline(transform, deckUi);
    }

    private void Update()
    {
        bool isGameplayNow = GameStateManager.IsGameplay;
        if (!isGameplayNow && _wasGameplayState)
            CancelFlyAndSnapCardsToHand();
        else if (isGameplayNow && !_wasGameplayState)
            RefreshFromLastKnownHandState();

        _wasGameplayState = isGameplayNow;
    }

    private void OnEnable()
    {
        StopFlyRoutineIfAny();
        HandCardViewCollection.DestroyAllSpawnedCardViews(handPanel, _spawnedViews);
        _lastHandOrder.Clear();

        if (battleSystem == null)
            return;

        battleSystem.HandChanged += RefreshHand;
    }

    private void OnDisable()
    {
        StopFlyRoutineIfAny();
        HandCardViewCollection.DestroyAllSpawnedCardViews(handPanel, _spawnedViews);

        if (battleSystem == null)
            return;

        battleSystem.HandChanged -= RefreshHand;
    }

    private void StopFlyRoutineIfAny()
    {
        if (_flyRoutine == null)
            return;

        StopCoroutine(_flyRoutine);
        _flyRoutine = null;
        _currentlyFlying.Clear();
        deckUi?.EndDeckCountFlyStagger();
    }

    private void CancelFlyAndSnapCardsToHand()
    {
        if (_flyRoutine != null)
        {
            StopCoroutine(_flyRoutine);
            _flyRoutine = null;
        }

        HandCardViewCollection.SnapFlyingCardsToHand(handPanel, _currentlyFlying);

        _currentlyFlying.Clear();

        var handRect = handPanel as RectTransform;
        if (handRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(handRect);
        }

        HandCardViewCollection.RefreshDragHierarchyForChildren(handPanel);

        deckUi?.EndDeckCountFlyStagger();
    }

    private void RefreshHand(DeckManager deckManager, int? selectedCardIndex)
    {
        if (battleSystem == null)
            return;

        _lastDeckManager = deckManager;
        _lastSelectedCardIndex = selectedCardIndex;

        if (deckManager == null || deckManager.Hand == null)
        {
            StopFlyRoutineIfAny();
            HandCardViewCollection.DestroyAllSpawnedCardViews(handPanel, _spawnedViews);
            _lastHandOrder.Clear();
            return;
        }

        if (deckManager.Hand.Count == 0)
        {
            StopFlyRoutineIfAny();
            HandCardViewCollection.DestroyAllSpawnedCardViews(handPanel, _spawnedViews);
            _lastHandOrder.Clear();
            return;
        }

        IReadOnlyList<CardData> newHand = deckManager.Hand.Cards;
        if (_flyRoutine != null && IsSameHandSnapshot(newHand))
            return;

        StopFlyRoutineIfAny();
        HandCardViewCollection.DestroyAllSpawnedCardViews(handPanel, _spawnedViews);

        bool suffixDraw = HandCardSequence.TryComputeSuffixDraw(_lastHandOrder, newHand, out int drawCount);

        HandCardViewCollection.CreateCardViews(handPanel, _spawnedViews, deckManager, cardPrefab, battleSystem, selectedCardIndex);

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
        {
            HandCardViewCollection.HideNewestCards(_spawnedViews, drawCount);
        }

        if (handRect != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(handRect);
        }

        if (shouldFly)
        {
            // Snapshot is updated before animation start to avoid re-triggering
            // the same fly sequence when duplicate HandChanged arrives.
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

    private void RefreshFromLastKnownHandState()
    {
        if (_lastDeckManager == null || _lastDeckManager.Hand == null)
            return;

        RefreshHand(_lastDeckManager, _lastSelectedCardIndex);
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
