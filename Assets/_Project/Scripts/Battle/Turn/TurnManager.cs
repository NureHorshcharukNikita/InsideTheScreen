using System;
using UnityEngine;
using System.Collections.Generic;

// Whose turn it is and what happens at the start and end of each. The player's side moves first:
// the player plays cards, then each ally acts, then each enemy; BattleSystem plays the fighters'
// turns one by one (BeginAiTurn, ExecuteAiAbility, EndAiTurn) to animate them.
public class TurnManager
{
    private PlayerCharacter player;
    private readonly IReadOnlyList<EnemyCharacter> allies;
    private readonly IReadOnlyList<EnemyCharacter> enemies;
    private DeckManager deckManager;

    public TurnOwner CurrentTurn { get; private set; }

    public IReadOnlyList<EnemyCharacter> Allies => allies;
    public IReadOnlyList<EnemyCharacter> Enemies => enemies;

    public bool AllEnemiesDefeated
    {
        get
        {
            foreach (EnemyCharacter enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                    return false;
            }

            return true;
        }
    }

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

    public TurnManager(
        PlayerCharacter player,
        IReadOnlyList<EnemyCharacter> allies,
        IReadOnlyList<EnemyCharacter> enemies,
        DeckManager deckManager)
    {
        this.player = player;
        this.allies = allies ?? Array.Empty<EnemyCharacter>();
        this.enemies = enemies ?? Array.Empty<EnemyCharacter>();
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

        foreach (EnemyCharacter fighter in GetAiFighters())
        {
            fighter.ApplyStartingStatuses();
            if (fighter.Brain != null)
                fighter.Brain.BindBattle(this);
        }

        PlanNextAiActions();
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

        // Points first, so turn-start statuses such as Stun can take some of them away.
        player.RestoreActionPoints();
        TickStatus(player, BattleStatusTiming.TurnStart);
        deckManager.DrawCards(player.CardsDrawnPerTurn);
    }

    public bool TryBeginEnemyTurn()
    {
        if (CurrentTurn != TurnOwner.Player)
            return false;

        DevLog.Log("Player turn ended");
        TickStatus(player, BattleStatusTiming.TurnEnd);
        CurrentTurn = TurnOwner.Enemy;
        return true;
    }

    // Allies first, then enemies: the order they act in.
    public List<EnemyCharacter> GetAiFighters()
    {
        var fighters = new List<EnemyCharacter>(allies.Count + enemies.Count);
        foreach (EnemyCharacter ally in allies)
        {
            if (ally != null)
                fighters.Add(ally);
        }

        foreach (EnemyCharacter enemy in enemies)
        {
            if (enemy != null)
                fighters.Add(enemy);
        }

        return fighters;
    }

    public void BeginAiTurn(EnemyCharacter fighter)
    {
        DevLog.Log($"{fighter.DisplayName}'s turn");
        TickStatus(fighter, BattleStatusTiming.TurnStart);
    }

    public bool ExecuteAiAbility(EnemyCharacter fighter, EnemyAbilityData ability)
    {
        if (fighter?.Brain == null || ability == null || !fighter.IsAlive || player == null || !player.IsAlive
            || GetEnemiesOf(fighter).Count == 0)
            return false;

        fighter.Brain.ExecuteAbility(ability);
        return true;
    }

    public void EndAiTurn(EnemyCharacter fighter)
    {
        if (fighter?.Brain == null)
            return;

        fighter.Brain.FinishTurn();
        TickStatus(fighter, BattleStatusTiming.TurnEnd);
    }

    public void PlanNextAiActions()
    {
        foreach (EnemyCharacter fighter in GetAiFighters())
        {
            if (fighter.IsAlive && fighter.Brain != null)
                fighter.Brain.PlanNextAction();
        }
    }

    public IReadOnlyList<ICombatant> GetAlliesOf(ICombatant self)
    {
        return GetTeam(self, sameTeam: true);
    }

    public IReadOnlyList<ICombatant> GetEnemiesOf(ICombatant self)
    {
        return GetTeam(self, sameTeam: false);
    }

    // The living fighters on self's side, or on the other side.
    private IReadOnlyList<ICombatant> GetTeam(ICombatant self, bool sameTeam)
    {
        if (self == null)
            return Array.Empty<ICombatant>();

        var team = new List<ICombatant>(PendingBattleEnemy.MaxPerSide);
        AddIfOnTeam(team, player, self, sameTeam);
        foreach (EnemyCharacter fighter in GetAiFighters())
            AddIfOnTeam(team, fighter, self, sameTeam);

        return team;
    }

    private static void AddIfOnTeam(List<ICombatant> team, Character fighter, ICombatant self, bool sameTeam)
    {
        if (fighter != null && fighter.IsAlive && (fighter.Team == self.Team) == sameTeam)
            team.Add(fighter);
    }

    private void TickStatus(ICombatant combatant, BattleStatusTiming timing)
    {
        if (combatant == null || !combatant.IsAlive)
            return;

        BattleActionContext actionContext = BuildActionContext();
        actionContext.Status.TickStatuses(combatant, timing, actionContext);
    }
}
