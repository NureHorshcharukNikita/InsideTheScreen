public interface IDamageService
{
    void ApplyDamage(ICombatant source, IDamageable target, int amount);

    // armorPierce: the share of the damage (0..1) that ignores armor.
    void ApplyDamage(ICombatant source, IDamageable target, int amount, float armorPierce);

    // A tick of burning, poison or bleeding: not a blow, so skills for blows don't count.
    void ApplyDamageOverTime(ICombatant source, IDamageable target, int amount);
}
