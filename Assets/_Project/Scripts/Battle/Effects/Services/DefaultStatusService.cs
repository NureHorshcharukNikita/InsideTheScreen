public sealed class DefaultStatusService : IStatusService
{
    public void ApplyStatus(
        ICombatant source,
        IStatusReceiver target,
        BattleStatus status,
        int duration,
        int stacks,
        int value,
        BattleStatusReapplyMode reapplyMode,
        BattleActionContext actionContext)
    {
        if (target == null || status == null)
            return;

        target.ApplyStatus(status, duration, stacks, value, source, reapplyMode, actionContext);
    }

    public void TickStatuses(IStatusReceiver target, BattleStatusTiming timing, BattleActionContext actionContext)
    {
        target?.TickStatuses(timing, actionContext);
    }
}
