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
        bool canEdit = GameStateManager.CanEditCardCollections();

        preview.Show(
            card,
            false,
            canEdit && deckController.HasDeck() && deckController.CanAdd(card),
            canEdit && inDeck);
    }

    public void ShowFromDeck(CardData card)
    {
        if (card == null)
            return;

        bool canEdit = GameStateManager.CanEditCardCollections();

        preview.Show(
            card,
            true,
            canEdit && deckController.CanAdd(card),
            canEdit);
    }

    public CardData GetSelected()
    {
        return preview.SelectedCard;
    }
}
