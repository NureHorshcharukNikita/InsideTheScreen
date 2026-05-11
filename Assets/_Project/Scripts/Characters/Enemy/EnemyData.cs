using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Health")]
    public int maxHealth = 30;

    [Header("Abilities")]
    [Tooltip("Pool used by EnemyBrain to pick the next planned ability (weighted).")]
    public List<EnemyAbilityData> abilities = new();
}