using System.Collections.Generic;

public interface IInstantEffectSpec
{
    InstantBattleEffect Effect { get; }
    BattleTargetingProfile Targeting { get; }
    int Value { get; }
    float ApplyChance { get; }
    IReadOnlyList<BattleEffectConditionSpec> Conditions { get; }
}
