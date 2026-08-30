using UnityEngine;

public abstract class StatusEffect : ScriptableObject
{
    [Header("Status Info")]
    [SerializeField] private string statusID;
    [SerializeField] private string statusName;

    [Header("Display")]
    [SerializeField] private BattleEffectDisplayData displayData;

    public string StatusID => statusID;
    public string StatusName => statusName;

    public BattleEffectDisplayEntry CreateDisplayEntry(ActiveStatusEffect status)
    {
        string fallbackTitle = string.IsNullOrWhiteSpace(statusName) ? name : statusName;
        if (displayData != null)
        {
            return displayData.CreateEntry(
                fallbackTitle,
                "Active status.",
                status.Value,
                status.Duration,
                status.Stacks);
        }

        string fallbackLabel = string.IsNullOrWhiteSpace(fallbackTitle) ? "S" : fallbackTitle[..1];
        string badgeValue = status.Stacks > 1
            ? status.Stacks.ToString()
            : status.Duration.ToString();

        return new BattleEffectDisplayEntry(
            null,
            fallbackLabel,
            badgeValue,
            fallbackTitle,
            "Active status.");
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
