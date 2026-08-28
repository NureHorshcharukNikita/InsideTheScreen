using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Effects/Gain Armor")]
public class GainArmorEffect : BattleEffect
{
    public override void Execute(ICombatant source, ICombatant target, int value, BattleActionContext actionContext)
    {
        if (target == null || actionContext == null)
            return;

        actionContext.Armor.AddArmor(source, target, value);
    }
}
