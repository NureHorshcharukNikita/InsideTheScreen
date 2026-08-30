using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Statuses/Regeneration", fileName = "RegenerationStatus")]
public class RegenerationStatus : BattleStatus
{
    [SerializeField] private BattleStatusTiming tickTiming = BattleStatusTiming.TurnEnd;

    public override void Tick(ActiveBattleStatus status, ICombatant target, BattleStatusTiming timing, BattleActionContext actionContext)
    {
        if (status == null || target == null || actionContext == null || timing != tickTiming)
            return;

        int healing = status.Value * Mathf.Max(status.Stacks, 1);
        actionContext.Heal.ApplyHeal(status.Source, target, healing);
    }
}
