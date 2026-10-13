using System;

internal sealed class BattleSystemPlayerActions
{
    private readonly Func<DeckManager> getDeckManager;
    private readonly Func<CardPlayer> getCardPlayer;
    private readonly Func<TurnManager> getTurnManager;
    private readonly Func<bool> canPlay;
    private readonly Action afterAction;
    private readonly Action notifyHandChanged;
    private readonly Action requestEndPlayerTurn;

    public BattleSystemPlayerActions(
        Func<DeckManager> getDeckManager,
        Func<CardPlayer> getCardPlayer,
        Func<TurnManager> getTurnManager,
        Func<bool> canPlay,
        Action afterAction,
        Action notifyHandChanged,
        Action requestEndPlayerTurn)
    {
        this.getDeckManager = getDeckManager;
        this.getCardPlayer = getCardPlayer;
        this.getTurnManager = getTurnManager;
        this.canPlay = canPlay;
        this.afterAction = afterAction;
        this.notifyHandChanged = notifyHandChanged;
        this.requestEndPlayerTurn = requestEndPlayerTurn;
    }

    public bool TryPlayCardFromHand(int index, ICombatant target)
    {
        if (!canPlay())
            return false;

        DeckManager deckManager = getDeckManager();
        CardPlayer cardPlayer = getCardPlayer();
        if (deckManager == null || cardPlayer == null)
            return false;

        if (index < 0 || index >= deckManager.Hand.Count)
            return false;

        CardData card = deckManager.Hand.Cards[index];

        if (cardPlayer.TryPlayCard(index, card, target))
        {
            afterAction();
            notifyHandChanged();
            return true;
        }

        return false;
    }

    public void EndTurn()
    {
        if (!canPlay())
            return;

        TurnManager turnManager = getTurnManager();
        DeckManager deckManager = getDeckManager();
        if (turnManager == null || deckManager == null)
            return;

        if (turnManager.CurrentTurn != TurnOwner.Player)
            return;

        requestEndPlayerTurn?.Invoke();
    }
}
