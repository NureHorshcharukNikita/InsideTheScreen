using System.Collections.Generic;
using TMPro;
using UnityEngine;

public partial class InventoryWindow : MonoBehaviour
{
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private PlayerHPText playerHPText;

    [SerializeField] private Transform inventoryCardsContent;
    [SerializeField] private Transform deckCardsContent;
    [SerializeField] private CardView cardViewPrefab;

    [SerializeField] private TMP_Text inventoryCountText;
    [SerializeField] private TMP_Text deckCountText;

    [SerializeField] private InventoryPreviewPanel inventoryPreviewPanel;
    [SerializeField] private InventoryCategoriesPanel categoriesPanel;

    [Header("Top Icons")]
    [SerializeField] private List<GameObject> topIconBorders = new();
    [SerializeField] private List<GameObject> pages = new();

    private InventoryScreenController inventoryScreenController;
    private int selectedTopIconIndex = -1;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        if (player == null)
        {
            Debug.LogError("Player not assigned in InventoryWindow");
            return;
        }

        var inventoryStorage = player.InventoryData;
        var deck = player.DeckData;

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
        if (playerHPText != null)
            playerHPText.SetTarget(player);

        categoriesPanel.Build(
            player.InventoryData.Cards,
            category =>
            {
                inventoryScreenController.SetCategory(category);
                Refresh();
            });

        inventoryPreviewPanel.Clear();
        
        SelectTopIcon(0);
        Refresh();
    }

    private void OnDisable()
    {
        if (GameStateManager.State == GameState.Inventory)
            GameStateManager.State = GameState.Gameplay;
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
