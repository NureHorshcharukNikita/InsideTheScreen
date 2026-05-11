public interface ICombatantInfo
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }
    CombatTeam Team { get; }
}
