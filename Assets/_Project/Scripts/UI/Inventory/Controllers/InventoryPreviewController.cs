using System.Linq;

public class InventoryPreviewController
{
    private readonly InventoryPreviewPanel preview;
    private readonly InventoryData storage;
    private readonly InventoryDeckController deck;

    public InventoryPreviewController(
        InventoryPreviewPanel preview,
        InventoryData storage,
        InventoryDeckController deck)
    {
        this.preview = preview;
        this.storage = storage;
        this.deck = deck;
    }

    public void Update()
    {
        var card = preview.SelectedCard;

        if (card == null)
            return;

        bool inInventory =
            storage.Cards.Contains(card);

        bool inDeck =
            deck.IsInDeck(card);

        if (!inInventory && !inDeck)
        {
            preview.Clear();
            return;
        }

        preview.Show(
            card,
            inDeck,
            deck.HasDeck() && deck.CanAdd(card),
            inDeck);
    }
}