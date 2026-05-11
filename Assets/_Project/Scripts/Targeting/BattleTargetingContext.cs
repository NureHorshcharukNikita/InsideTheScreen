using System.Collections.Generic;

public readonly struct BattleTargetingContext
{
    public BattleTargetingContext(
        ICombatant self,
        ICombatant selectedTarget,
        IReadOnlyList<ICombatant> allies,
        IReadOnlyList<ICombatant> enemies)
    {
        Self = self;
        SelectedTarget = selectedTarget;
        Allies = allies ?? System.Array.Empty<ICombatant>();
        Enemies = enemies ?? System.Array.Empty<ICombatant>();
    }

    public ICombatant Self { get; }
    public ICombatant SelectedTarget { get; }
    public IReadOnlyList<ICombatant> Allies { get; }
    public IReadOnlyList<ICombatant> Enemies { get; }
}
