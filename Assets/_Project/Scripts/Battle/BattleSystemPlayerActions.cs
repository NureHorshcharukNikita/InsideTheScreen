public partial class BattleSystem
{
    public void OnTargetClicked(ICombatant target)
    {
        if (!CanPlay())
            return;

        if (selectedCardIndex == null)
            return;

        TryPlayCardFromHand(selectedCardIndex.Value, target);
    }

    public bool TryPlayCardFromHand(int index, ICombatant target)
    {
        if (!CanPlay())
            return false;

        if (index < 0 || index >= deckManager.Hand.Count)
            return false;

        var card = deckManager.Hand.Cards[index];

        if (cardPlayer.TryPlayCard(index, card, target))
        {
            DevLog.Log("Played: " + card.CardName);

            BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
            BattleDebugPrinter.PrintCards("Discard", deckManager.DiscardPile.Cards);

            selectedCardIndex = null;
            AfterAction();
            NotifyHandChanged();
            return true;
        }

        return false;
    }

    public void EndTurn()
    {
        if (!CanPlay())
            return;

        if (turnManager == null || deckManager == null)
            return;

        if (turnManager.CurrentTurn != TurnOwner.Player)
            return;

        turnManager.EndPlayerTurn();

        BattleDebugPrinter.PrintCards("Hand", deckManager.Hand.Cards);
        AfterAction();

        selectedCardIndex = null;

        NotifyHandChanged();
    }

    public void SelectCard(int index)
    {
        if (!CanPlay())
            return;

        if (index < 0 || index >= deckManager.Hand.Count)
            return;

        selectedCardIndex = index;

        DevLog.Log("Selected card: " + deckManager.Hand.Cards[index].CardName);
    }

    public void DeselectCard()
    {
        if (selectedCardIndex == null)
            return;

        selectedCardIndex = null;
        DevLog.Log("Card deselected");
    }
}
