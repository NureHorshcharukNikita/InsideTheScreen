using UnityEngine;

public abstract class BattleStatus : ScriptableObject
{
    [Header("Status Info")]
    [SerializeField] private string statusID;
    [SerializeField] private string statusName;

    [Header("Display")]
    [SerializeField] private BattleEffectDisplayData displayData;

    [Header("Reapply")]
    [SerializeField] private BattleStatusReapplyMode reapplyMode = BattleStatusReapplyMode.AddStacks;

    public string StatusID => statusID;
    public string StatusName => statusName;
    public BattleStatusReapplyMode ReapplyMode => reapplyMode;

    public BattleEffectDisplayEntry CreateDisplayEntry(ActiveBattleStatus status)
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

    public virtual void OnApply(ActiveBattleStatus status, ICombatant target, BattleActionContext actionContext)
    {
    }

    public virtual void Tick(ActiveBattleStatus status, ICombatant target, BattleStatusTiming timing, BattleActionContext actionContext)
    {
    }

    public virtual void OnExpire(ActiveBattleStatus status, ICombatant target, BattleActionContext actionContext)
    {
    }
}
