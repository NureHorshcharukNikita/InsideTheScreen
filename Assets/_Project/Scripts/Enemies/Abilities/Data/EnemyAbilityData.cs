using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Enemies/Enemy Ability", fileName = "NewEnemyAbility")]
public class EnemyAbilityData : ScriptableObject
{
    [Header("Identity")]
    public string abilityID;

    public string displayName;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    [Header("Selection")]
    [Min(1)]
    public int selectionWeight = 10;

    public string intentSummary;

    [Min(0)]
    public int cooldownTurns = 0;

    [Min(-1)]
    public int maxUses = -1;

    [Header("Gameplay")]
    public List<BattleCondition> conditions = new();
    public List<EnemyAbilityEffectSpec> effects = new();
}
