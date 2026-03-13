using UnityEngine;

public static class BattleStateChecker
{
    public static void Check(PlayerCharacter player, EnemyCharacter enemy)
    {
        if (enemy.CurrentHealth <= 0)
        {
            DevLog.Log("Enemy defeated!");
        }

        if (player.CurrentHealth <= 0)
        {
            DevLog.Log("Player defeated!");
        }
    }
}