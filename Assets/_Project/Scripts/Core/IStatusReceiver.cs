public interface IStatusReceiver
{
    void ApplyStatus(StatusEffect status, int duration, int stacks, int value, ICombatant source, BattleActionContext actionContext);
    void TickStatuses(BattleStatusTiming timing, BattleActionContext actionContext);
}
