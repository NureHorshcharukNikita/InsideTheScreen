public sealed class BattleActionContext
{
    private static readonly IDamageService DefaultDamage = new DefaultDamageService();
    private static readonly IHealService DefaultHeal = new DefaultHealService();

    public BattleActionContext(IDamageService damageService = null, IHealService healService = null)
    {
        Damage = damageService ?? DefaultDamage;
        Heal = healService ?? DefaultHeal;
    }

    public IDamageService Damage { get; }
    public IHealService Heal { get; }

    public static BattleActionContext CreateDefault()
    {
        return new BattleActionContext();
    }
}
