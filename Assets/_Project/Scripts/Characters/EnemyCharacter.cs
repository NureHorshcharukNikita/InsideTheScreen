using UnityEngine;

public class EnemyCharacter : Character
{
    [SerializeField] private EnemyData enemyData;

    public override int MaxHealth => enemyData != null ? enemyData.maxHealth : 0;

    public EnemyData Data => enemyData;
}