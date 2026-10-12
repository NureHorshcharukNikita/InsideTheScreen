using System;
using System.Collections.Generic;
using UnityEngine;

public class StatusController : MonoBehaviour, IBattleEffectDisplaySource
{
    private readonly List<ActiveBattleStatus> statuses = new();

    public IReadOnlyList<ActiveBattleStatus> Statuses => statuses;

    public event Action StatusesChanged;

    public void CollectDisplayEntries(List<BattleEffectDisplayEntry> entries)
    {
        for (int i = 0; i < statuses.Count; i++)
        {
            ActiveBattleStatus status = statuses[i];
            if (status?.Status == null || status.Status.IsHiddenFromPlayer
                || (status.Duration <= 0 && !status.Status.IsPermanent))
                continue;

            entries.Add(status.Status.CreateDisplayEntry(status));
        }
    }

    public void ApplyStatus(BattleStatus status, int duration, int stacks, int value, ICombatant source, ICombatant target, BattleStatusReapplyMode reapplyMode, BattleActionContext actionContext)
    {
        if (status == null || target == null)
            return;

        ActiveBattleStatus existing = statuses.Find(activeStatus => activeStatus.Status == status);
        if (existing != null)
        {
            existing.Refresh(duration, stacks, value, source, reapplyMode);
            status.OnApply(existing, target, actionContext);
            StatusesChanged?.Invoke();
            return;
        }

        ActiveBattleStatus activeStatus = new(status, duration, stacks, value, source);
        statuses.Add(activeStatus);
        status.OnApply(activeStatus, target, actionContext);
        StatusesChanged?.Invoke();
    }

    public void TickStatuses(ICombatant target, BattleStatusTiming timing, BattleActionContext actionContext)
    {
        if (target == null)
            return;

        bool changed = false;

        // A snapshot: a tick can change the list (bleeding damage triggering a death ward that
        // clears every status).
        ActiveBattleStatus[] snapshot = statuses.ToArray();
        for (int i = snapshot.Length - 1; i >= 0; i--)
        {
            ActiveBattleStatus activeStatus = snapshot[i];
            if (!statuses.Contains(activeStatus))
                continue;

            activeStatus.Status.Tick(activeStatus, target, timing, actionContext);
            if (!statuses.Contains(activeStatus) || activeStatus.Status.IsPermanent)
                continue;

            if (timing == BattleStatusTiming.TurnEnd)
            {
                activeStatus.ReduceDuration(activeStatus.Status.ExpirationMode);
                changed = true;
            }

            if (!activeStatus.IsExpired)
                continue;

            activeStatus.Status.OnExpire(activeStatus, target, actionContext);
            statuses.Remove(activeStatus);
        }

        if (changed)
            StatusesChanged?.Invoke();
    }

    public bool HasStatus(BattleStatus status)
    {
        return status != null && statuses.Exists(activeStatus => activeStatus.Status == status);
    }

    // Asks the bearer's statuses, in order, whether one of them keeps them alive; that one is used up.
    public bool TryPreventDeath(Character bearer, out BattleStatus preventedBy)
    {
        preventedBy = null;
        if (bearer == null)
            return false;

        foreach (ActiveBattleStatus activeStatus in statuses.ToArray())
        {
            if (activeStatus?.Status == null || !activeStatus.Status.TryPreventDeath(activeStatus, bearer, this))
                continue;

            statuses.Remove(activeStatus);
            preventedBy = activeStatus.Status;
            StatusesChanged?.Invoke();
            return true;
        }

        return false;
    }

    public void RemoveAllStatuses()
    {
        if (statuses.Count == 0)
            return;

        statuses.Clear();
        StatusesChanged?.Invoke();
    }
}
