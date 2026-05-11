using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Health")]
    public int maxHealth = 30;

    [Header("Battle UI")]
    [Tooltip("HP bar anchor along sprite bounds: 0 = bottom (bar under the art). Increase slightly for very tall sprites so the bar clears feet/limbs.")]
    [Range(0f, 1f)]
    public float healthBarAnchorAlongSprite = 0f;

    [Header("Battle placement")]
    [Tooltip("Added to this enemy's localPosition.y after the encounter template is applied (battle only). Use a small positive value if feet sit lower than the player.")]
    public float battleLocalPositionYOffset = 0f;

    [Header("Abilities")]
    [Tooltip("Pool used by EnemyBrain to pick the next planned ability (weighted).")]
    public List<EnemyAbilityData> abilities = new();
}