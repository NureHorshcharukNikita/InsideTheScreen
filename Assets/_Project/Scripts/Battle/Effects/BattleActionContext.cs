public sealed class BattleActionContext
{
    public IDamageService Damage { get; } = new DefaultDamageService();
    public IHealService Heal { get; } = new DefaultHealService();
    public IArmorService Armor { get; } = new DefaultArmorService();
    public IStatusService Status { get; } = new DefaultStatusService();

    public static BattleActionContext CreateDefault()
    {
        return new BattleActionContext();
    }
}
