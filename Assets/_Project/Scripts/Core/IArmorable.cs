public interface IArmorable
{
    int Armor { get; }
    void AddArmor(int amount);
    int AbsorbDamageWithArmor(int amount);
    void ClearArmor();
}
