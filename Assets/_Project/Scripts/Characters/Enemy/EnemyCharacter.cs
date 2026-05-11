using UnityEngine;

public class EnemyCharacter : Character
{
    [SerializeField] private EnemyData enemyData;
    private EnemyBrain brain;

    public override int MaxHealth => enemyData != null ? enemyData.maxHealth : 0;
    public override CombatTeam Team => CombatTeam.Enemy;

    public EnemyData Data => enemyData;
    public EnemyBrain Brain => brain != null ? brain : (brain = GetComponent<EnemyBrain>());
}