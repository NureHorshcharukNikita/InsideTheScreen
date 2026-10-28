using UnityEngine;

// Draws Value cards from the deck into Haru's hand, e.g. a belt with pouches.
[CreateAssetMenu(menuName = "Game/Battle/Instant Effects/Draw Cards")]
public class DrawCardsEffect : InstantBattleEffect
{
    public override void Execute(ICombatant source, ICombatant target, int value, BattleActionContext actionContext)
    {
        if (value <= 0 || source is not PlayerCharacter)
            return;

        actionContext?.DrawCards?.Invoke(value);
    }
}
