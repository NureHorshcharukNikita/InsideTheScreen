public sealed class DefaultArmorService : IArmorService
{
    public void AddArmor(ICombatant source, IArmorable target, int amount)
    {
        if (target == null || amount < 0)
            return;

        target.AddArmor(amount);
    }

    public void ClearArmor(IArmorable target)
    {
        target?.ClearArmor();
    }
}
