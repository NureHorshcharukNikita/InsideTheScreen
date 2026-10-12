public sealed class DefaultDamageService : IDamageService
{
    public void ApplyDamage(ICombatant source, IDamageable target, int amount)
    {
        ApplyDamage(source, target, amount, 0f);
    }

    public void ApplyDamage(ICombatant source, IDamageable target, int amount, float armorPierce)
    {
        if (target == null || amount < 0)
            return;

        if (source is PlayerCharacter)
            amount = UnityEngine.Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.PhysicalDamage, amount));

        if (target is PlayerCharacter)
            amount = UnityEngine.Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.PhysicalDamageTaken, amount));

        // The pierced share goes past the armor; the rest is soaked up as usual.
        int pierced = UnityEngine.Mathf.CeilToInt(amount * UnityEngine.Mathf.Clamp01(armorPierce));
        int blockable = amount - pierced;
        int remainingDamage = target is IArmorable armorable
            ? armorable.AbsorbDamageWithArmor(blockable)
            : blockable;

        target.TakeDamage(pierced + remainingDamage);
    }
}
