using System.Collections.Generic;

public interface IBattleEffectSpec
{
    BattleEffect Effect { get; }
    BattleTargetingProfile Targeting { get; }
    int Value { get; }
    float ApplyChance { get; }
    IReadOnlyList<BattleCondition> Conditions { get; }
}
