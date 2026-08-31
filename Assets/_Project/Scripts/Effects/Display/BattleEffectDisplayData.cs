using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Effect Display")]
public class BattleEffectDisplayData : ScriptableObject
{
    [Header("Badge")]
    [SerializeField] private Sprite icon;
    [SerializeField] private string fallbackLabel;
    [SerializeField] private BattleEffectBadgeMode badgeMode = BattleEffectBadgeMode.ValueSource;
    [SerializeField] private BattleEffectBadgeValueSource badgeValueSource = BattleEffectBadgeValueSource.Value;
    [SerializeField] private string badgeValueFormat = "{value}";

    [Header("Tooltip")]
    [SerializeField] private string title;

    [TextArea(2, 5)]
    [SerializeField] private string tooltipFormat;

    public Sprite Icon => icon;
    public string FallbackLabel => fallbackLabel;

    public BattleEffectDisplayEntry CreateEntry(
        string fallbackTitle,
        string fallbackTooltip,
        int value,
        int duration,
        int stacks)
    {
        string displayTitle = string.IsNullOrWhiteSpace(title) ? fallbackTitle : title;
        string tooltipTemplate = string.IsNullOrWhiteSpace(tooltipFormat) ? fallbackTooltip : tooltipFormat;
        return new BattleEffectDisplayEntry(
            icon,
            fallbackLabel,
            CreateBadgeValue(value, duration, stacks),
            displayTitle,
            BattleEffectDisplayFormatter.Format(tooltipTemplate, value, duration, stacks));
    }

    private string CreateBadgeValue(int value, int duration, int stacks)
    {
        int badgeValue = badgeValueSource switch
        {
            BattleEffectBadgeValueSource.Duration => duration,
            BattleEffectBadgeValueSource.Stacks => stacks,
            BattleEffectBadgeValueSource.StacksThenDuration => stacks > 1 ? stacks : duration,
            _ => value
        };

        string badgeTemplate = badgeMode == BattleEffectBadgeMode.CustomFormat
            ? badgeValueFormat
            : "{badge}";
        if (string.IsNullOrWhiteSpace(badgeTemplate))
            badgeTemplate = "{badge}";

        return BattleEffectDisplayFormatter.Format(badgeTemplate, value, duration, stacks, badgeValue);
    }
}
