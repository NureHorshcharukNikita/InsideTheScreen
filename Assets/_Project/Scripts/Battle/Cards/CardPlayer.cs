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

    public bool TryPlayCard(int index, CardData card, ICombatant target)
    {
        if (card == null)
            return false;

        if (turnManager.CurrentTurn != TurnOwner.Player)
        {
            DevLog.Log("It is not player's turn");
            return false;
        }

        // A Bow is held, not played; an Arrow flies only while the Bow is in hand.
        if (card.IsHeldInHand)
        {
            DevLog.Log($"{card.CardName} is held, not played");
            return false;
        }

        if (card.NeedsInHand != null && !deckManager.Hand.Contains(card.NeedsInHand))
        {
            DevLog.Log($"{card.CardName} needs {card.NeedsInHand.CardName} in hand");
            return false;
        }

        BattleTargetingContext context = turnManager.BuildTargetingContext(player, target);
        if (!CardResolver.CanResolveAnyTarget(card, context))
            return false;

        int actionPointCost = player.GetActionPointCost(card);
        if (!player.SpendActionPoints(actionPointCost))
        {
            DevLog.Log("Not enough action points");
            return false;
        }

        BattleActionContext actionContext = turnManager.BuildActionContext();
        CardResolver.Resolve(card, context, actionContext);

        deckManager.DiscardByIndexFromHand(index);
        player.NoteCardPlayed();

        return true;
    }
}
