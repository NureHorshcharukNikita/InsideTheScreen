using System.Collections.Generic;

public interface IStatusApplicationSpec
{
    BattleStatus Status { get; }
    BattleTargetingProfile Targeting { get; }
    int Value { get; }
    int Duration { get; }
    int Stacks { get; }
    float ApplyChance { get; }
    IReadOnlyList<BattleEffectConditionSpec> Conditions { get; }
}
