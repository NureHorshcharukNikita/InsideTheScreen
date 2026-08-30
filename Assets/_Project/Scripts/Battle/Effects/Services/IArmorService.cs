public interface IArmorService
{
    void AddArmor(ICombatant source, IArmorable target, int amount);
    void ClearArmor(IArmorable target);
}
