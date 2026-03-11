using UnityEngine;

public class CardPlayer
{
    private PlayerCharacter player;
    private DeckManager deckManager;
    private TurnManager turnManager;

    public CardPlayer(PlayerCharacter player, DeckManager deckManager, TurnManager turnManager)
    {
        this.player = player;
        this.deckManager = deckManager;
        this.turnManager = turnManager;
    }

    public bool TryPlayCard(CardData card, IEffectTarget target)
    {
        if (card == null)
            return false;

        if (turnManager.CurrentTurn != TurnOwner.Player)
        {
            Debug.Log("It is not player's turn");
            return false;
        }

        if (!CanUseCardOnTarget(card, target))
        {
            Debug.Log($"{card.CardName} cannot be used on this target");
            return false;
        }

        if (!player.SpendActionPoints(card.Cost))
        {
            Debug.Log("Not enough action points");
            return false;
        }

        CardResolver.Resolve(card, player, target);

        deckManager.Discard(card);

        return true;
    }

    private bool CanUseCardOnTarget(CardData card, IEffectTarget target)
    {
        foreach (var entry in card.Effects)
        {
            if (entry.effect == null)
                continue;

            if (entry.targetType == EffectTargetType.Enemy && target is EnemyCharacter)
                return true;

            if (entry.targetType == EffectTargetType.Self && target is PlayerCharacter)
                return true;
        }

        return false;
    }
}