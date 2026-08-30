public interface IHealService
{
    void ApplyHeal(ICombatant source, IHealable target, int amount);
}
