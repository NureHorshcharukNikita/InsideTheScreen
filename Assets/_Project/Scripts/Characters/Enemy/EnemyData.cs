using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyID;
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Header("Health")]
    public int maxHealth = 30;

    [Header("Abilities")]
    public List<EnemyAbilityData> abilities = new();
}
