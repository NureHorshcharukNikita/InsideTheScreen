using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyAbilityEffectSpec : IBattleEffectSpec
{
    public BattleEffect effect;

    public BattleTargetingProfile targeting;

    [Min(0)]
    public int value;

    [Range(0f, 1f)]
    public float applyChance = 1f;

    public List<BattleCondition> conditions = new();

    public BattleEffect Effect => effect;
    public BattleTargetingProfile Targeting => targeting;
    public int Value => value;
    public float ApplyChance => applyChance;
    public IReadOnlyList<BattleCondition> Conditions => conditions;
}
