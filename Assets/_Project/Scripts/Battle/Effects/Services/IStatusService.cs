public interface IStatusService
{
    void ApplyStatus(
        ICombatant source,
        IStatusReceiver target,
        BattleStatus status,
        int duration,
        int stacks,
        int value,
        BattleStatusReapplyMode reapplyMode,
        BattleActionContext actionContext);

    void TickStatuses(IStatusReceiver target, BattleStatusTiming timing, BattleActionContext actionContext);
}
