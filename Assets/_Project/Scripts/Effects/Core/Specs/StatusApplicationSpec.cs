using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatusApplicationSpec : IStatusApplicationSpec
{
    public BattleStatus status;

    public BattleTargetingProfile targeting;

    [Min(0)]
    public int value;

    [Min(0)]
    public int duration;

    [Min(1)]
    public int stacks = 1;

    [Range(0f, 1f)]
    public float applyChance = 1f;

    public StatusApplicationReapplyMode reapplyMode;

    public List<BattleEffectConditionSpec> conditions = new();

    public BattleStatus Status => status;
    public BattleTargetingProfile Targeting => targeting;
    public int Value => value;
    public int Duration => duration;
    public int Stacks => stacks;
    public float ApplyChance => applyChance;
    public StatusApplicationReapplyMode ReapplyMode => reapplyMode;
    public IReadOnlyList<BattleEffectConditionSpec> Conditions => conditions;
}
