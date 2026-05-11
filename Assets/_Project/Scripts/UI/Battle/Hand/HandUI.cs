using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

public partial class HandUI : MonoBehaviour
{
    public event Action DrawFlyAnimationCompleted;

    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private DeckUI deckUi;

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

    public void ReleaseAllBattleCardDrags()
    {
        Canvas rootCanvas = handPanel.GetComponentInParent<Canvas>()?.rootCanvas;
        if (rootCanvas != null)
        {
            RectTransform rootRect = rootCanvas.transform as RectTransform;
            if (rootRect != null)
            {
                for (int i = 0; i < rootRect.childCount; i++)
                {
                    CardView cardView = rootRect.GetChild(i).GetComponent<CardView>();
                    if (cardView != null)
                        cardView.ForceReleaseBattleDragToHand();
                }
            }
        }

        for (int i = 0; i < _spawnedViews.Count; i++)
        {
            if (_spawnedViews[i] != null)
                _spawnedViews[i].ForceReleaseBattleDragToHand();
        }

        HandCardViewCollection.RefreshDragHierarchyForChildren(handPanel);
    }

    private void RefreshFromLastKnownHandState()
    {
        if (_lastDeckManager == null || _lastDeckManager.Hand == null)
            return;

        RefreshHand(_lastDeckManager, _lastSelectedCardIndex);
    }

}
