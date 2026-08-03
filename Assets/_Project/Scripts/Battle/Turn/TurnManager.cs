using System;
using UnityEngine;
using System.Collections.Generic;

public class TurnManager
{
    private PlayerCharacter player;
    private EnemyCharacter enemy;
    private DeckManager deckManager;

    public event Action AfterEnemyActed;

    public TurnOwner CurrentTurn { get; private set; }

    public BattleTargetingContext BuildTargetingContext(ICombatant self, ICombatant selectedTarget)
    {
        IReadOnlyList<ICombatant> allies = GetAlliesOf(self);
        IReadOnlyList<ICombatant> enemies = GetEnemiesOf(self);
        return new BattleTargetingContext(self, selectedTarget, allies, enemies);
    }

    public BattleActionContext BuildActionContext()
    {
        return BattleActionContext.CreateDefault();
    }

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

        if (enemy?.Brain != null)
        {
            enemy.Brain.BindOpponent(player);
            enemy.Brain.PlanNextAction();
        }
    }

    public void DrawStartingHand()
    {
        if (deckManager == null)
            return;

        deckManager.DrawCards(player.StartHandSize);
    }

    public void StartNextPlayerTurn()
    {
        CurrentTurn = TurnOwner.Player;

        DevLog.Log("New player turn started");

        player.RestoreActionPoints();
        deckManager.DrawCards(player.CardsDrawnPerTurn);
    }

    public bool TryBeginEnemyTurn()
    {
        if (CurrentTurn != TurnOwner.Player)
            return false;

        DevLog.Log("Player turn ended");
        CurrentTurn = TurnOwner.Enemy;
        return true;
    }

    public void ExecuteEnemyTurn()
    {
        DevLog.Log("Enemy turn");

        if (enemy?.Brain != null)
        {
            if (enemy.Brain.CurrentPlan.HasAbility)
                enemy.Brain.ExecutePlanned();

            AfterEnemyActed?.Invoke();
        }
    }

    public void PlanNextEnemyAction()
    {
        if (enemy?.Brain != null)
            enemy.Brain.PlanNextAction();
    }

    private IReadOnlyList<ICombatant> GetAlliesOf(ICombatant self)
    {
        if (self == null)
            return System.Array.Empty<ICombatant>();

        var allies = new List<ICombatant>(2);
        if (player != null && player.Team == self.Team && player.IsAlive)
            allies.Add(player);
        if (enemy != null && enemy.Team == self.Team && enemy.IsAlive)
            allies.Add(enemy);
        return allies;
    }

    private IReadOnlyList<ICombatant> GetEnemiesOf(ICombatant self)
    {
        if (self == null)
            return System.Array.Empty<ICombatant>();

        var enemies = new List<ICombatant>(2);
        if (player != null && player.Team != self.Team && player.IsAlive)
            enemies.Add(player);
        if (enemy != null && enemy.Team != self.Team && enemy.IsAlive)
            enemies.Add(enemy);
        return enemies;
    }
}