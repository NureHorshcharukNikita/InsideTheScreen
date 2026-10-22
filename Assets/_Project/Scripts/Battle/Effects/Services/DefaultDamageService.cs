public sealed class DefaultDamageService : IDamageService
{
    public void ApplyDamage(ICombatant source, IDamageable target, int amount)
    {
        ApplyDamage(source, target, amount, 0f);
    }

    // A hit: Haru's skills add to the blows he deals (Strength, Aggression when low on health) and
    // take off the ones he gets (Armor), and Counterattack strikes back at whoever hit him.
    public void ApplyDamage(ICombatant source, IDamageable target, int amount, float armorPierce)
    {
        if (target == null || amount < 0)
            return;

        if (source is PlayerCharacter attacker && amount > 0)
        {
            amount = SkillProgression.ModifyInt(SkillStat.PhysicalDamage, amount);
            if (attacker.IsLowOnHealth)
                amount = SkillProgression.ModifyInt(SkillStat.LowHealthDamage, amount);
        }

        if (target is PlayerCharacter && amount > 0)
            amount = SkillProgression.ModifyInt(SkillStat.PhysicalDamageTaken, amount);

        Hit(target, UnityEngine.Mathf.Max(0, amount), armorPierce);

        if (target is PlayerCharacter defender && defender.IsAlive && amount > 0
            && source is EnemyCharacter { Team: CombatTeam.Enemy } enemy && enemy.IsAlive
            && SkillProgression.HasPassive(SkillPassive.Counterattack))
            Hit(enemy, Balance.Config.counterattackDamage, 0f);
    }

    // Burning, poison, bleeding: no Strength or Armor; Emotional Echo adds to Haru's own.
    public void ApplyDamageOverTime(ICombatant source, IDamageable target, int amount)
    {
        if (target == null || amount < 0)
            return;

        if (source is PlayerCharacter && amount > 0)
            amount = UnityEngine.Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.DamageOverTimeDone, amount));

        Hit(target, amount, 0f);
    }

    // The pierced share goes past the armor; the rest is soaked up as usual.
    private static void Hit(IDamageable target, int amount, float armorPierce)
    {
        int pierced = UnityEngine.Mathf.CeilToInt(amount * UnityEngine.Mathf.Clamp01(armorPierce));
        int blockable = amount - pierced;
        int remainingDamage = target is IArmorable armorable
            ? armorable.AbsorbDamageWithArmor(blockable)
            : blockable;

        target.TakeDamage(pierced + remainingDamage);
    }
}
