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
            if (status?.Status == null || status.Duration <= 0)
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
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            ActiveBattleStatus activeStatus = statuses[i];
            activeStatus.Status.Tick(activeStatus, target, timing, actionContext);

            if (timing == BattleStatusTiming.TurnEnd)
            {
                activeStatus.ReduceDuration();
                changed = true;
            }

            if (!activeStatus.IsExpired)
                continue;

            activeStatus.Status.OnExpire(activeStatus, target, actionContext);
            statuses.RemoveAt(i);
        }

        if (changed)
            StatusesChanged?.Invoke();
    }
}
