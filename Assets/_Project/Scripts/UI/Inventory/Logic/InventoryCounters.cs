using TMPro;

public static class InventoryCounters
{
    public static void Update(
        TMP_Text inventoryText,
        TMP_Text deckText,
        InventoryData storage,
        DeckData deck)
    {
        int inventoryCount = storage.Cards.Count;
        inventoryText.text = $"{inventoryCount}/60";

        if (deck != null)
        {
            deckText.text =
                $"{deck.Cards.Count}/{deck.MaxCount}";
        }
        else
        {
            deckText.text = "0/0";
        }
    }

    public static void SetEmpty(
        TMP_Text inventoryText,
        TMP_Text deckText)
    {
        inventoryText.text = "0/0";
        deckText.text = "0/0";
    }
}