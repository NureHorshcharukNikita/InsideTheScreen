using UnityEngine;

public static class BattleStateChecker
{
    public static void Check(PlayerCharacter player, EnemyCharacter enemy, BattleEndUI battleEndUI)
    {
        if (enemy.CurrentHealth <= 0)
        {
            DevLog.Log("Enemy defeated!");
            battleEndUI.ShowVictory();
            return;
        }

        if (player.CurrentHealth <= 0)
        {
            DevLog.Log("Player defeated!");
            battleEndUI.ShowDefeat();
        }
    }
}