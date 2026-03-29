using UnityEngine;

public class TurnManager
{
    private PlayerCharacter player;
    private EnemyCharacter enemy;
    private DeckManager deckManager;

    public TurnOwner CurrentTurn { get; private set; }

    public TurnManager(PlayerCharacter player, EnemyCharacter enemy, DeckManager deckManager)
    {
        this.player = player;
        this.enemy = enemy;
        this.deckManager = deckManager;
    }
    public void StartBattle()
    {
        if (deckManager == null)
        {
            DevLog.Log("DeckManager is null");
            return;
        }

        DevLog.Log("Battle started");

        CurrentTurn = TurnOwner.Player;

        deckManager.DrawCards(5);

        //player.AddActionPoints(4);
    }

    public void StartNextPlayerTurn()
    {
        CurrentTurn = TurnOwner.Player;

        DevLog.Log("New player turn started");

        player.RestoreActionPoints();
        deckManager.DrawCards(1);
    }

    public void EndPlayerTurn()
    {
        if (CurrentTurn != TurnOwner.Player)
            return;

        DevLog.Log("Player turn ended");

        CurrentTurn = TurnOwner.Enemy;

        ExecuteEnemyTurn();
        
        StartNextPlayerTurn();
    }

    private void ExecuteEnemyTurn()
    {
        DevLog.Log("Enemy turn");

        //enemy.Attack(player);
        player.TakeDamage(5);
    }
}