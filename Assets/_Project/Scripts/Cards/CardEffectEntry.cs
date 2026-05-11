using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CardEffectEntry
{
    public BattleEffect effect;
    public int value;
    public BattleTargetingProfile targeting;
    [Range(0f, 1f)]
    public float applyChance = 1f;
    public List<BattleCondition> conditions = new();
}
