using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleEffectSpec : IBattleEffectSpec
{
    public BattleEffect effect;

    public StatusEffect status;

    public BattleTargetingProfile targeting;

    [Min(0)]
    public int value;

    [Min(0)]
    public int duration;

    [Min(0)]
    public int stacks = 1;

    [Range(0f, 1f)]
    public float applyChance = 1f;

    public List<BattleCondition> conditions = new();

    public BattleEffect Effect => effect;
    public StatusEffect Status => status;
    public BattleTargetingProfile Targeting => targeting;
    public int Value => value;
    public int Duration => duration;
    public int Stacks => stacks;
    public float ApplyChance => applyChance;
    public IReadOnlyList<BattleCondition> Conditions => conditions;
}
