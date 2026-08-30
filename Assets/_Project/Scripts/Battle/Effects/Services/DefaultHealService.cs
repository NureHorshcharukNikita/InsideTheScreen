public sealed class DefaultHealService : IHealService
{
    public void ApplyHeal(ICombatant source, IHealable target, int amount)
    {
        if (target == null || amount < 0)
            return;

        target.Heal(amount);
    }
}
