using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Enemies/Enemy Ability", fileName = "NewEnemyAbility")]
public class EnemyAbilityData : ScriptableObject
{
    [Tooltip("Short name for logs; intent text uses Intent Summary or Display Name if set.")]
    public string displayName;

    [Tooltip("Optional icon for UI (intent bubble, etc.).")]
    public Sprite icon;

    [Min(1)]
    [Tooltip("Relative weight when EnemyBrain picks a random next ability.")]
    public int selectionWeight = 10;

    [Tooltip("Text shown above the enemy for the planned action. If empty, Display Name or asset name is used.")]
    public string intentSummary;

    [Min(0)]
    [Tooltip("Turns to wait before this ability can be used again after execution.")]
    public int cooldownTurns = 0;

    [Min(-1)]
    [Tooltip("-1 = unlimited uses; otherwise max uses per battle.")]
    public int maxUses = -1;

    public List<BattleCondition> conditions = new();
    public List<EnemyAbilityEffectSpec> effects = new();
}
