using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Instant Effects/Damage")]
public class DamageEffect : InstantBattleEffect
{
    public override void Execute(ICombatant source, ICombatant target, int value, BattleActionContext actionContext)
    {
        if (target == null || actionContext == null)
            return;

        actionContext.Damage.ApplyDamage(source, target, value);
    }
}
