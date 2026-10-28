public sealed class DefaultHealService : IHealService
{
    // Equilibrium makes every heal Haru gives a little stronger; a bandana or a bracelet on the one
    // healed, every heal they get (Healing Boost).
    public void ApplyHeal(ICombatant source, IHealable target, int amount)
    {
        if (target == null || amount < 0)
            return;

        if (source is PlayerCharacter && amount > 0)
            amount = UnityEngine.Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.HealingDone, amount));

        if (target is ICombatant healed)
            amount = HealingBoostStatus.Boost(healed, amount);

        target.Heal(amount);
    }
}
