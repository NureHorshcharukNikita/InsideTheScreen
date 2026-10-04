public sealed class DefaultDamageService : IDamageService
{
    public void ApplyDamage(ICombatant source, IDamageable target, int amount)
    {
        if (target == null || amount < 0)
            return;

        if (source is PlayerCharacter)
            amount = UnityEngine.Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.PhysicalDamage, amount));

        if (target is PlayerCharacter)
            amount = UnityEngine.Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.PhysicalDamageTaken, amount));

        int remainingDamage = target is IArmorable armorable
            ? armorable.AbsorbDamageWithArmor(amount)
            : amount;

        target.TakeDamage(remainingDamage);
    }
}
