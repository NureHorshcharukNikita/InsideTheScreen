using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Instant Effects/Heal")]
public class HealEffect : InstantBattleEffect
{
    public override void Execute(ICombatant source, ICombatant target, int value, BattleActionContext actionContext)
    {
        if (target == null || actionContext == null)
            return;

        actionContext.Heal.ApplyHeal(source, target, value);
    }
}
