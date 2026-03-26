public class InventorySelectionPresenter
{
    private readonly InventoryPreviewPanel preview;
    private readonly InventoryDeckController deckController;

    public InventorySelectionPresenter(
        InventoryPreviewPanel preview,
        InventoryDeckController deckController)
    {
        this.preview = preview;
        this.deckController = deckController;
    }

    public void ShowFromInventory(CardData card)
    {
        if (card == null)
            return;

        bool inDeck = deckController.IsInDeck(card);

        preview.Show(
            card,
            false,
            deckController.HasDeck() && deckController.CanAdd(card),
            inDeck);
    }

    public void ShowFromDeck(CardData card)
    {
        if (card == null)
            return;

        preview.Show(
            card,
            true,
            deckController.CanAdd(card),
            true);
    }

    public CardData GetSelected()
    {
        return preview.SelectedCard;
    }
}