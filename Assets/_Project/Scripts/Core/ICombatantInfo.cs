public interface ICombatantInfo
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    int Armor { get; }
    bool IsAlive { get; }
    CombatTeam Team { get; }
}
