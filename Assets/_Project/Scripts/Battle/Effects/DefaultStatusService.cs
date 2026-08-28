public sealed class DefaultStatusService : IStatusService
{
    public void ApplyStatus(
        ICombatant source,
        IStatusReceiver target,
        StatusEffect status,
        int duration,
        int stacks,
        int value,
        BattleActionContext actionContext)
    {
        if (target == null || status == null)
            return;

        target.ApplyStatus(status, duration, stacks, value, source, actionContext);
    }

    public void TickStatuses(IStatusReceiver target, BattleStatusTiming timing, BattleActionContext actionContext)
    {
        target?.TickStatuses(timing, actionContext);
    }
}
