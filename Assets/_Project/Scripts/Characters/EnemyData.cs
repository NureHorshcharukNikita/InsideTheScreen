using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Health")]
    public int maxHealth = 30;

    //[Header("Abilities")]
    //public List<EnemyAbility> abilities = new();
}