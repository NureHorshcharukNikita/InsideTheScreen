using UnityEngine;

// The first time the bearer would die, they rise again instead: back to a share of their health,
// cleansed of every status, and optionally changed by a new one (a second phase).
// Make it permanent so it stays visible as a warning until it is used.
[CreateAssetMenu(menuName = "Game/Battle/Statuses/Death Ward", fileName = "DeathWardStatus")]
public class DeathWardStatus : BattleStatus
{
    [Header("Revive")]
    [SerializeField, Range(0.05f, 1f)] private float reviveHealthShare = 0.5f;
    [SerializeField] private bool clearStatusesOnRevive = true;
    [Tooltip("Applied for the rest of the battle after reviving, e.g. a 'second phase' marker.")]
    [SerializeField] private BattleStatus statusAfterRevive;

    [Header("Presentation")]
    [Tooltip("Shown across the screen when the ward triggers.")]
    [SerializeField] private string announcement;

    public string Announcement => announcement;

    public override bool TryPreventDeath(ActiveBattleStatus status, Character bearer, StatusController statuses)
    {
        bearer.SetHealth(Mathf.Max(1, Mathf.CeilToInt(bearer.MaxHealth * reviveHealthShare)));

        if (clearStatusesOnRevive)
            statuses.RemoveAllStatuses();

        if (statusAfterRevive != null)
            bearer.ApplyStatus(statusAfterRevive, 0, 1, 0, bearer, statusAfterRevive.ReapplyMode, BattleActionContext.CreateDefault());

        return true;
    }
}
