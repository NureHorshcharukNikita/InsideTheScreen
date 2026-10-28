public sealed class BattleActionContext
{
    public IDamageService Damage { get; } = new DefaultDamageService();
    public IHealService Heal { get; } = new DefaultHealService();
    public IArmorService Armor { get; } = new DefaultArmorService();
    public IStatusService Status { get; } = new DefaultStatusService();
    // Draws this many cards into Haru's hand; nothing outside a battle.
    public System.Action<int> DrawCards { get; set; }

    public static BattleActionContext CreateDefault()
    {
        return new BattleActionContext();
    }
}
