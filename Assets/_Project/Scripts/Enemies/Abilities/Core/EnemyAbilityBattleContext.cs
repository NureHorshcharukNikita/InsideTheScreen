using System.Collections.Generic;

public sealed class EnemyAbilityBattleContext
{
    public EnemyAbilityBattleContext(EnemyCharacter self, PlayerCharacter player)
    {
        Self = self;
        Player = player;
    }

    public EnemyCharacter Self { get; }
    public PlayerCharacter Player { get; }

    public IReadOnlyList<Character> GetAllies()
    {
        return Self != null ? new[] { (Character)Self } : System.Array.Empty<Character>();
    }

    public IReadOnlyList<Character> GetEnemies()
    {
        return Player != null ? new[] { (Character)Player } : System.Array.Empty<Character>();
    }
}
