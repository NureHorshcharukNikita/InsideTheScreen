public sealed class DefaultDamageService : IDamageService
{
    public void ApplyDamage(ICombatant source, IDamageable target, int amount)
    {
        if (target == null || amount < 0)
            return;

        target.TakeDamage(amount);
    }
}
