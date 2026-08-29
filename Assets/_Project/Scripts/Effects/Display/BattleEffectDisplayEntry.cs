using UnityEngine;

public readonly struct BattleEffectDisplayEntry
{
    public BattleEffectDisplayEntry(Sprite icon, string fallbackLabel, string badgeValue, string title, string tooltip)
    {
        Icon = icon;
        FallbackLabel = fallbackLabel;
        BadgeValue = badgeValue;
        Title = title;
        Tooltip = tooltip;
    }

    public Sprite Icon { get; }
    public string FallbackLabel { get; }
    public string BadgeValue { get; }
    public string Title { get; }
    public string Tooltip { get; }
}
