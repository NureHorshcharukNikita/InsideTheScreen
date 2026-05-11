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

        if (!CanUseCardOnTarget(card, target))
        {
            DevLog.Log($"{card.CardName} cannot be used on this target");
            return false;
        }

        if (!player.SpendActionPoints(card.Cost))
        {
            DevLog.Log("Not enough action points");
            return false;
        }

        BattleTargetingContext ctx = turnManager.BuildTargetingContext(player, target);
        BattleActionContext actionCtx = turnManager.BuildActionContext();
        CardResolver.Resolve(card, ctx, actionCtx);

        deckManager.DiscardByIndexFromHand(index);

        return true;
    }

    private bool CanUseCardOnTarget(CardData card, ICombatant target)
    {
        BattleTargetingContext ctx = turnManager.BuildTargetingContext(player, target);
        return CardResolver.CanResolveAnyTarget(card, ctx);
    }
}