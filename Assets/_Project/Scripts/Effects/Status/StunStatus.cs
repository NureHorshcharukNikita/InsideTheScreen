using UnityEngine;

// Dazed: at the start of their next turn the bearer has {total} fewer action points
// (value × stacks). The player starts the turn with less to spend; an enemy drops actions it planned.
[CreateAssetMenu(menuName = "Game/Battle/Statuses/Stun", fileName = "StunStatus")]
public class StunStatus : BattleStatus
{
    public override void Tick(ActiveBattleStatus status, ICombatant target, BattleStatusTiming timing, BattleActionContext actionContext)
    {
        if (status == null || timing != BattleStatusTiming.TurnStart)
            return;

        int lostPoints = Mathf.Max(1, status.Value) * Mathf.Max(1, status.Stacks);
        if (target is PlayerCharacter player)
            player.LoseActionPoints(lostPoints);
        else if (target is EnemyCharacter enemy && enemy.Brain != null)
            enemy.Brain.LoseActionPoints(lostPoints);
    }
}
