using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Instant Effects/Damage")]
public class DamageEffect : InstantBattleEffect
{
    [Header("Damage")]
    [Tooltip("Share of the damage that ignores armor: 0.5 = half of the hit goes past armor.")]
    [SerializeField, Range(0f, 1f)] private float armorPierce;

    public float ArmorPierce => armorPierce;

    public override void Execute(ICombatant source, ICombatant target, int value, BattleActionContext actionContext)
    {
        if (target == null || actionContext == null)
            return;

        actionContext.Damage.ApplyDamage(source, target, value, armorPierce);
    }
}
