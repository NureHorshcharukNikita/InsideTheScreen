public sealed class BattleActionContext
{
    public IDamageService Damage { get; } = new DefaultDamageService();
    public IHealService Heal { get; } = new DefaultHealService();

    public static BattleActionContext CreateDefault()
    {
        return new BattleActionContext();
    }
}
