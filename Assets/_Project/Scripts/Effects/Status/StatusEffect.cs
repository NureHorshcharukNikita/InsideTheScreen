using UnityEngine;

public abstract class StatusEffect : ScriptableObject
{
    [Header("Status Info")]
    [SerializeField] private string statusID;
    [SerializeField] private string statusName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    [SerializeField] private Sprite icon;

    [Header("Display")]
    [SerializeField] private BattleEffectDisplayData displayData;

    public string StatusID => statusID;
    public string StatusName => statusName;
    public string Description => description;
    public Sprite Icon => icon;

    public BattleEffectDisplayEntry CreateDisplayEntry(ActiveStatusEffect status)
    {
        string fallbackTitle = string.IsNullOrWhiteSpace(statusName) ? name : statusName;
        string fallbackLabel = string.IsNullOrWhiteSpace(fallbackTitle) ? "S" : fallbackTitle[..1];
        string badgeValue = status.Stacks > 1
            ? status.Stacks.ToString()
            : status.Duration.ToString();

        if (displayData != null)
        {
            return displayData.CreateEntry(
                fallbackTitle,
                description,
                status.Value,
                status.Duration,
                status.Stacks);
        }

        return new BattleEffectDisplayEntry(
            icon,
            fallbackLabel,
            badgeValue,
            fallbackTitle,
            description);
    }

    public virtual void OnApply(ActiveStatusEffect status, ICombatant target, BattleActionContext actionContext)
    {
    }

    public virtual void Tick(ActiveStatusEffect status, ICombatant target, BattleStatusTiming timing, BattleActionContext actionContext)
    {
    }

    public virtual void OnExpire(ActiveStatusEffect status, ICombatant target, BattleActionContext actionContext)
    {
    }
}
