using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class HealthBarUI
{
    private const float EffectCellWidth = 55f;
    private const float EffectCellHeight = 49f;
    private static readonly Vector2 EffectSpacing = new(6f, 5f);

    private RectTransform effectsRoot;
    private GridLayoutGroup effectsGrid;
    private readonly List<GameObject> effectBadges = new();

    private void InitializeEffectsView()
    {
        EnsureEffectsRoot();
    }

    private void SubscribeEffects(Character character)
    {
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

        GameObject root = new("EffectBadges", typeof(RectTransform));
        root.transform.SetParent(background != null ? background : _rect, false);

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

        if (target.Armor > 0)
        {
            AddEffectBadge(
                armorIcon,
                "A",
                target.Armor.ToString(),
                "Armor",
                $"Blocks incoming damage before health.\nCurrent armor: {target.Armor}");
        }

        foreach (ActiveStatusEffect status in target.Statuses)
        {
            if (status?.Status == null)
                continue;
            if (status.Duration <= 0)
                continue;

            string value = status.Stacks > 1
                ? status.Stacks.ToString()
                : status.Duration.ToString();
            string title = string.IsNullOrWhiteSpace(status.Status.StatusName)
                ? status.Status.name
                : status.Status.StatusName;
            string description = string.IsNullOrWhiteSpace(status.Status.Description)
                ? "Active effect."
                : status.Status.Description;
            string body = $"{description}\nDuration: {status.Duration}\nStacks: {status.Stacks}";

            AddEffectBadge(status.Status.Icon, "S", value, title, body);
        }

        UpdateEffectsLayout();
        effectsRoot.gameObject.SetActive(effectBadges.Count > 0);
    }

    private void AddEffectBadge(Sprite icon, string fallbackLabel, string value, string title, string body)
    {
        GameObject badgeObject = new(title, typeof(RectTransform));
        badgeObject.transform.SetParent(effectsRoot, false);

        BattleEffectBadgeUI badge = badgeObject.AddComponent<BattleEffectBadgeUI>();
        badge.Build(icon, fallbackLabel, value, title, body);

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
