public sealed class DefaultHealService : IHealService
{
    // Equilibrium makes every heal Haru gives a little stronger.
    public void ApplyHeal(ICombatant source, IHealable target, int amount)
    {
        if (target == null || amount < 0)
            return;

        if (source is PlayerCharacter && amount > 0)
            amount = UnityEngine.Mathf.Max(0, SkillProgression.ModifyInt(SkillStat.HealingDone, amount));

        target.Heal(amount);
    }
}
