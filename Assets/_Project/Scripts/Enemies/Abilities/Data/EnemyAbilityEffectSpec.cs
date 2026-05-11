using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyAbilityEffectSpec
{
    [Tooltip("Shared effect asset used by cards and enemies.")]
    public BattleEffect effect;

    [Tooltip("Target selection strategy asset. If empty, falls back to single enemy.")]
    public BattleTargetingProfile targeting;

    [Min(0)]
    [Tooltip("Value passed into Effect.Execute(..., value).")]
    public int amount = 5;

    [Range(0f, 1f)]
    [Tooltip("Chance this effect applies when the ability is executed.")]
    public float applyChance = 1f;

    public List<BattleCondition> conditions = new();
}
