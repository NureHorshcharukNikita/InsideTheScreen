using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameplayHudFadeState : MonoBehaviour
{
    private const string MinimapRootName = "MinimapMask";

    private static readonly Dictionary<Graphic, Color> SavedColors = new();
    private static readonly Dictionary<Graphic, bool> SavedRaycastTargets = new();

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Range(0f, 1f)] private float gameplayAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float blockedAlpha = 0.5f;

    private Graphic[] graphics;
    private Color[] initialColors;
    private bool[] initialRaycastTargets;

    public static void SetMinimapBlocked(bool blocked)
    {
        GameObject minimapRoot = GameObject.Find(MinimapRootName);
        if (minimapRoot == null)
            return;

        ApplyGraphicsState(
            minimapRoot.GetComponentsInChildren<Graphic>(true),
            !blocked,
            1f,
            0.5f);
    }

    private void Reset()
    {
        ResolveCanvasGroup();
        ResolveGraphics();
    }

    private void Awake()
    {
        ResolveCanvasGroup();
        ResolveGraphics();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveCanvasGroup();
        ResolveGraphics();
        Refresh();
    }

    private void OnTransformChildrenChanged()
    {
        ResolveGraphics();
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void ResolveCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void ResolveGraphics()
    {
        graphics = GetComponentsInChildren<Graphic>(true);
        initialColors = new Color[graphics.Length];
        initialRaycastTargets = new bool[graphics.Length];

        for (int i = 0; i < graphics.Length; i++)
        {
            initialColors[i] = graphics[i].color;
            initialRaycastTargets[i] = graphics[i].raycastTarget;
        }
    }

    private void Refresh()
    {
        bool enabledInGameplay = GameStateManager.IsGameplay;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = enabledInGameplay;
            canvasGroup.blocksRaycasts = enabledInGameplay;
        }

        if (graphics == null || initialColors == null || initialRaycastTargets == null)
            return;

        ApplyGraphicsState(
            graphics,
            enabledInGameplay,
            gameplayAlpha,
            blockedAlpha,
            initialColors,
            initialRaycastTargets);
    }

    private static void ApplyGraphicsState(
        Graphic[] targetGraphics,
        bool enabledInGameplay,
        float gameplayAlpha,
        float blockedAlpha,
        Color[] originalColors = null,
        bool[] originalRaycastTargets = null)
    {
        if (targetGraphics == null)
            return;

        float targetAlpha = enabledInGameplay ? gameplayAlpha : blockedAlpha;
        for (int i = 0; i < targetGraphics.Length; i++)
        {
            Graphic graphic = targetGraphics[i];
            if (graphic == null)
                continue;

            if (!SavedColors.ContainsKey(graphic))
            {
                SavedColors[graphic] = originalColors != null && i < originalColors.Length
                    ? originalColors[i]
                    : graphic.color;
                SavedRaycastTargets[graphic] = originalRaycastTargets != null && i < originalRaycastTargets.Length
                    ? originalRaycastTargets[i]
                    : graphic.raycastTarget;
            }

            Color color = SavedColors[graphic];
            color.a *= targetAlpha;
            graphic.color = color;
            graphic.raycastTarget = enabledInGameplay && SavedRaycastTargets[graphic];
        }
    }
}
