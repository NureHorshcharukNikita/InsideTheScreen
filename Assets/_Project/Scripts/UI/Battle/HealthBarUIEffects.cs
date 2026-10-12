using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class HealthBarUI
{
    private const string EffectsRootName = "EffectBadges";
    private const float EffectCellWidth = 55f;
    private const float EffectCellHeight = 49f;
    private static readonly Vector2 EffectSpacing = new(6f, 5f);

    private RectTransform effectsRoot;
    private GridLayoutGroup effectsGrid;
    private readonly List<GameObject> effectBadges = new();
    private readonly List<IBattleEffectDisplaySource> effectDisplaySources = new();
    private readonly List<BattleEffectDisplayEntry> effectDisplayEntries = new();

    private void InitializeEffectsView()
    {
        EnsureEffectsRoot();
    }

    private void SubscribeEffects(Character character)
    {
        CacheEffectDisplaySources(character);
        character.ArmorChanged += RefreshEffects;
        character.StatusesChanged += RefreshEffects;
    }

    private void UnsubscribeEffects(Character character)
    {
        character.ArmorChanged -= RefreshEffects;
        character.StatusesChanged -= RefreshEffects;
    }

    private void EnsureEffectsRoot()
    {
        if (effectsRoot != null)
            return;

        // A copy of an HP bar made in battle (for an extra enemy or an ally) brings the
        // original's root along: use it instead of adding a second one.
        Transform parent = background != null ? background : _rect;
        Transform copiedRoot = parent.Find(EffectsRootName);
        if (copiedRoot != null && copiedRoot.TryGetComponent(out GridLayoutGroup copiedGrid))
        {
            effectsRoot = (RectTransform)copiedRoot;
            effectsGrid = copiedGrid;
            return;
        }

        GameObject root = new(EffectsRootName, typeof(RectTransform));
        root.transform.SetParent(parent, false);

        effectsRoot = (RectTransform)root.transform;
        effectsRoot.anchorMin = new Vector2(0f, 0f);
        effectsRoot.anchorMax = new Vector2(1f, 0f);
        effectsRoot.pivot = new Vector2(0.5f, 1f);
        effectsRoot.anchoredPosition = effectsOffset;
        effectsRoot.sizeDelta = new Vector2(0f, EffectCellHeight);

        effectsGrid = root.AddComponent<GridLayoutGroup>();
        effectsGrid.cellSize = new Vector2(EffectCellWidth, EffectCellHeight);
        effectsGrid.spacing = EffectSpacing;
        effectsGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        effectsGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        effectsGrid.childAlignment = TextAnchor.UpperLeft;
        effectsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        effectsGrid.constraintCount = GetEffectsColumnCount();

        root.SetActive(false);
    }

    private void RefreshEffects(int _)
    {
        RefreshEffects();
    }

    private void RefreshEffects()
    {
        EnsureEffectsRoot();
        ClearEffectBadges();

        if (target == null)
            return;

        CollectDisplayEntries();
        foreach (BattleEffectDisplayEntry entry in effectDisplayEntries)
            AddEffectBadge(entry);

        UpdateEffectsLayout();
        effectsRoot.gameObject.SetActive(effectBadges.Count > 0);
    }

    private void CacheEffectDisplaySources(Character character)
    {
        effectDisplaySources.Clear();
        character.GetComponents(effectDisplaySources);
    }

    private void CollectDisplayEntries()
    {
        if (effectDisplaySources.Count == 0 && target != null)
            CacheEffectDisplaySources(target);

        effectDisplayEntries.Clear();
        for (int i = 0; i < effectDisplaySources.Count; i++)
            effectDisplaySources[i]?.CollectDisplayEntries(effectDisplayEntries);
    }

    private void AddEffectBadge(BattleEffectDisplayEntry entry)
    {
        GameObject badgeObject = new(entry.Title, typeof(RectTransform));
        badgeObject.transform.SetParent(effectsRoot, false);

        BattleEffectBadgeUI badge = badgeObject.AddComponent<BattleEffectBadgeUI>();
        badge.Build(entry);

        effectBadges.Add(badgeObject);
    }

    private void UpdateEffectsLayout()
    {
        int columns = GetEffectsColumnCount();
        if (effectsGrid != null)
            effectsGrid.constraintCount = columns;

        int rows = effectBadges.Count == 0
            ? 1
            : Mathf.CeilToInt(effectBadges.Count / (float)columns);
        float height = rows * EffectCellHeight + Mathf.Max(0, rows - 1) * EffectSpacing.y;
        effectsRoot.sizeDelta = new Vector2(0f, height);
        effectsRoot.anchoredPosition = effectsOffset;
    }

    private int GetEffectsColumnCount()
    {
        float width = background != null && background.rect.width > 0f
            ? background.rect.width
            : 220f;
        return Mathf.Max(1, Mathf.FloorToInt((width + EffectSpacing.x) / (EffectCellWidth + EffectSpacing.x)));
    }

    private void ClearEffectBadges()
    {
        for (int i = 0; i < effectBadges.Count; i++)
        {
            if (effectBadges[i] != null)
                Object.Destroy(effectBadges[i]);
        }

        effectBadges.Clear();
    }
}
