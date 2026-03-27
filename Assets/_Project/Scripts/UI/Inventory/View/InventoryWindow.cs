using TMPro;
using UnityEngine;

public class InventoryWindow : MonoBehaviour
{
    [SerializeField] private PlayerData player;

    [SerializeField] private Transform inventoryCardsContent;
    [SerializeField] private Transform deckCardsContent;
    [SerializeField] private CardView cardViewPrefab;

    [SerializeField] private TMP_Text inventoryCountText;
    [SerializeField] private TMP_Text deckCountText;

    [SerializeField] private InventoryPreviewPanel inventoryPreviewPanel;
    [SerializeField] private InventoryCategoriesPanel categoriesPanel;

    private InventoryScreenController inventoryScreenController;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        if (player == null)
        {
            Debug.LogError("Player not assigned in InventoryWindow");
            return;
        }

        var inventoryStorage = player.Inventory;
        var deck = player.Deck;

        var inventoryDrawer = new CardCollectionDrawer(
            inventoryCardsContent,
            cardViewPrefab);

        var deckDrawer = new CardCollectionDrawer(
            deckCardsContent,
            cardViewPrefab);

        inventoryScreenController = new InventoryScreenController(
            inventoryStorage,
            deck,
            inventoryPreviewPanel,
            inventoryDrawer,
            deckDrawer);

        InventoryViewUtils.Cleanup(
            inventoryCardsContent,
            deckCardsContent);
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void OnEnable()
    {
        categoriesPanel.Build(
            player.Inventory.Cards,
            category =>
            {
                inventoryScreenController.SetCategory(category);
                Refresh();
            });

        Refresh();
        inventoryPreviewPanel.Clear();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
        inventoryPreviewPanel.Clear();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Refresh()
    {
        inventoryScreenController.Refresh(
            inventoryCountText,
            deckCountText);
    }

    public void AddSelectedToDeck()
    {
        inventoryScreenController.AddSelected();
        Refresh();
    }

    public void RemoveSelectedFromDeck()
    {
        inventoryScreenController.RemoveSelected();
        Refresh();
    }
}