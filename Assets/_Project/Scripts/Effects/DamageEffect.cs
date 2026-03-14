using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Damage")]
public class DamageEffect : CardEffect
{
    public override void Execute(IEffectTarget source, IEffectTarget target, int value)
    {
        if (target == null)
            return;

        target.TakeDamage(value);
    }
}