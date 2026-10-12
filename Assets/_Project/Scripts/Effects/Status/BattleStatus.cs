using UnityEngine;

public abstract class BattleStatus : ScriptableObject
{
    [Header("Status Info")]
    [SerializeField] private string statusID;
    [SerializeField] private string statusName;

    [Header("Display")]
    [SerializeField] private BattleEffectDisplayData displayData;
    [Tooltip("Not shown on the bearer's status bar: a secret the player finds out when it triggers.")]
    [SerializeField] private bool hiddenFromPlayer;

    [Header("Reapply")]
    [SerializeField] private BattleStatusReapplyMode reapplyMode = BattleStatusReapplyMode.AddStacks;

    [Header("Expiration")]
    [SerializeField] private BattleStatusExpirationMode expirationMode = BattleStatusExpirationMode.RemoveStatus;
    [Tooltip("Never runs out: a passive that lasts the whole battle (e.g. Seal of Immortality).")]
    [SerializeField] private bool permanent;

    public string StatusID => statusID;
    public string StatusName => statusName;
    public BattleStatusReapplyMode ReapplyMode => reapplyMode;
    public BattleStatusExpirationMode ExpirationMode => expirationMode;
    public bool IsPermanent => permanent;
    public bool IsHiddenFromPlayer => hiddenFromPlayer;

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

    // Called when the bearer's health is about to drop to 0. Return true to keep them alive;
    // the status is then used up.
    public virtual bool TryPreventDeath(ActiveBattleStatus status, Character bearer, StatusController statuses)
    {
        return false;
    }
}
