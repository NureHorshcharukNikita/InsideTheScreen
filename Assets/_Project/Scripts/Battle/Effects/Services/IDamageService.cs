public interface IDamageService
{
    void ApplyDamage(ICombatant source, IDamageable target, int amount);
}
