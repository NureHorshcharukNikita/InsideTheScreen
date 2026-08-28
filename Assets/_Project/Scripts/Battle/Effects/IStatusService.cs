public interface IStatusService
{
    void ApplyStatus(
        ICombatant source,
        IStatusReceiver target,
        StatusEffect status,
        int duration,
        int stacks,
        int value,
        BattleActionContext actionContext);

    void TickStatuses(IStatusReceiver target, BattleStatusTiming timing, BattleActionContext actionContext);
}
