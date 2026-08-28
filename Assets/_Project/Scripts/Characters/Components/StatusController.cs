using System.Collections.Generic;
using UnityEngine;

public class StatusController : MonoBehaviour
{
    private readonly List<ActiveStatusEffect> statuses = new();

    public void ApplyStatus(StatusEffect status, int duration, int stacks, int value, ICombatant source, ICombatant target, BattleActionContext actionContext)
    {
        if (status == null || target == null)
            return;

        ActiveStatusEffect existing = statuses.Find(activeStatus => activeStatus.Status == status);
        if (existing != null)
        {
            existing.Refresh(duration, stacks, value, source);
            status.OnApply(existing, target, actionContext);
            return;
        }

        ActiveStatusEffect activeStatus = new(status, duration, stacks, value, source);
        statuses.Add(activeStatus);
        status.OnApply(activeStatus, target, actionContext);
    }

    public void TickStatuses(ICombatant target, BattleStatusTiming timing, BattleActionContext actionContext)
    {
        if (target == null)
            return;

        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect activeStatus = statuses[i];
            activeStatus.Status.Tick(activeStatus, target, timing, actionContext);

            if (timing == BattleStatusTiming.TurnEnd)
                activeStatus.ReduceDuration();

            if (!activeStatus.IsExpired)
                continue;

            activeStatus.Status.OnExpire(activeStatus, target, actionContext);
            statuses.RemoveAt(i);
        }
    }
}
