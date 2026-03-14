using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Heal")]
public class HealEffect : CardEffect
{
    public override void Execute(IEffectTarget source, IEffectTarget target, int value)
    {
        if (target == null)
            return;

        target.Heal(value);
    }
}