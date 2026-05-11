using System;

internal sealed class BattleSystemPlayerActions
{
    private readonly Func<DeckManager> getDeckManager;
    private readonly Func<CardPlayer> getCardPlayer;
    private readonly Func<TurnManager> getTurnManager;
    private readonly Func<bool> canPlay;
    private readonly Action afterAction;
    private readonly Action notifyHandChanged;
    private readonly Func<int?> getSelectedCardIndex;
    private readonly Action<int?> setSelectedCardIndex;

    public BattleSystemPlayerActions(
        Func<DeckManager> getDeckManager,
        Func<CardPlayer> getCardPlayer,
        Func<TurnManager> getTurnManager,
        Func<bool> canPlay,
        Action afterAction,
        Action notifyHandChanged,
        Func<int?> getSelectedCardIndex,
        Action<int?> setSelectedCardIndex)
    {
        this.getDeckManager = getDeckManager;
        this.getCardPlayer = getCardPlayer;
        this.getTurnManager = getTurnManager;
        this.canPlay = canPlay;
        this.afterAction = afterAction;
        this.notifyHandChanged = notifyHandChanged;
        this.getSelectedCardIndex = getSelectedCardIndex;
        this.setSelectedCardIndex = setSelectedCardIndex;
    }

    public void OnTargetClicked(ICombatant target)
    {
        if (!canPlay())
            return;

        int? selectedCardIndex = getSelectedCardIndex();
        if (selectedCardIndex == null)
            return;

        TryPlayCardFromHand(selectedCardIndex.Value, target);
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
            DevLog.Log("Played: " + card.CardName);

            BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
            BattleDebugPrinter.PrintCards("Discard", deckManager.DiscardPile.Cards);

            setSelectedCardIndex(null);
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

        turnManager.EndPlayerTurn();

        BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
        afterAction();

        setSelectedCardIndex(null);

        notifyHandChanged();
    }

    public void SelectCard(int index)
    {
        if (!canPlay())
            return;

        DeckManager deckManager = getDeckManager();
        if (deckManager == null)
            return;

        if (index < 0 || index >= deckManager.Hand.Count)
            return;

        setSelectedCardIndex(index);

        DevLog.Log("Selected card: " + deckManager.Hand.Cards[index].CardName);
    }

    public void DeselectCard()
    {
        if (getSelectedCardIndex() == null)
            return;

        setSelectedCardIndex(null);
        DevLog.Log("Card deselected");
    }
}
