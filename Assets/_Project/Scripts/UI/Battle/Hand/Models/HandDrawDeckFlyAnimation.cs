using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public readonly struct HandFlyTweenSettings
{
    public readonly float DealStartDelay;
    public readonly float Duration;
    public readonly float Stagger;

    public HandFlyTweenSettings(
        float dealStartDelay,
        float duration,
        float stagger)
    {
        DealStartDelay = dealStartDelay;
        Duration = duration;
        Stagger = stagger;
    }
}

public readonly struct HandDrawDeckFlyContext
{
    public readonly Transform HandPanel;
    public readonly List<CardView> SpawnedViews;
    public readonly List<RectTransform> FlyingBuffer;
    public readonly List<CardData> SnapshotDestination;
    public readonly DeckManager DeckManager;
    public readonly RectTransform HandRect;
    public readonly RectTransform CanvasRoot;
    public readonly SplineContainer FlightSpline;
    public readonly HandFlyTweenSettings Settings;
    public readonly Action OnDeckFlyCardArrived;

    public HandDrawDeckFlyContext(
        Transform handPanel,
        List<CardView> spawnedViews,
        List<RectTransform> flyingBuffer,
        List<CardData> snapshotDestination,
        DeckManager deckManager,
        RectTransform handRect,
        RectTransform canvasRoot,
        SplineContainer flightSpline,
        HandFlyTweenSettings settings,
        Action onDeckFlyCardArrived = null)
    {
        HandPanel = handPanel;
        SpawnedViews = spawnedViews;
        FlyingBuffer = flyingBuffer;
        SnapshotDestination = snapshotDestination;
        DeckManager = deckManager;
        HandRect = handRect;
        CanvasRoot = canvasRoot;
        FlightSpline = flightSpline;
        Settings = settings;
        OnDeckFlyCardArrived = onDeckFlyCardArrived;
    }
}
