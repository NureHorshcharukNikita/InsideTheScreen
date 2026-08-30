using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InstantEffectSpec : IInstantEffectSpec
{
    public InstantBattleEffect effect;

    public BattleTargetingProfile targeting;

    [Min(0)]
    public int value;

    [Range(0f, 1f)]
    public float applyChance = 1f;

    public List<BattleEffectConditionSpec> conditions = new();

    public InstantBattleEffect Effect => effect;
    public BattleTargetingProfile Targeting => targeting;
    public int Value => value;
    public float ApplyChance => applyChance;
    public IReadOnlyList<BattleEffectConditionSpec> Conditions => conditions;
}
