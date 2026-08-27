using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InventoryWindow : MonoBehaviour
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

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject optionsPanel;

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

        ResetPauseMenu();
    }

    private void Update()
    {
        if (IsOpen && GameInputSettings.GetKeyDown(GameInputAction.Close))
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
            GameStateManager.SetState(GameState.Gameplay);
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

    public void Open()
    {
        GameStateManager.SetState(GameState.Inventory);
        gameObject.SetActive(true);

        inventoryScreenController.ClearSelection();

        inventoryPreviewPanel.Clear();
        ResetPauseMenu();
        SelectTopIcon(0);
        Refresh();
    }

    public void Close()
    {
        inventoryScreenController.ClearSelection();

        GameStateManager.SetState(GameState.Gameplay);
        gameObject.SetActive(false);
    }

    public void ResumeGame()
    {
        ClearSelectedButton();
        ResetPauseMenu();
    }

    public void ShowOptions()
    {
        ClearSelectedButton();
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    public void HideOptions()
    {
        ClearSelectedButton();
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    public void SaveGame()
    {
        ClearSelectedButton();
        SaveCurrentSession();
    }

    public void QuitGame()
    {
        ClearSelectedButton();
        SaveCurrentSession();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void SelectTopIcon(int index)
    {
        if (index < 0 || index >= topIconBorders.Count)
            return;

        selectedTopIconIndex = index;

        categoriesPanel.ResetToAll();

        inventoryScreenController.ClearSelection();
        Refresh();

        for (int i = 0; i < topIconBorders.Count; i++)
        {
            if (topIconBorders[i] != null)
                topIconBorders[i].SetActive(i == index);
        }

        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == index);
        }
    }

    public void SelectNextTopIcon()
    {
        if (topIconBorders.Count == 0)
            return;

        int next = selectedTopIconIndex + 1;
        if (next >= topIconBorders.Count)
            next = 0;

        SelectTopIcon(next);
    }

    public void SelectPreviousTopIcon()
    {
        if (topIconBorders.Count == 0)
            return;

        int prev = selectedTopIconIndex - 1;
        if (prev < 0)
            prev = topIconBorders.Count - 1;

        SelectTopIcon(prev);
    }

    public void ReturnToMainMenu()
    {
        SaveCurrentSession();
        MainMenuNavigation.GoToMainMenu();
    }

    private void SaveCurrentSession()
    {
        PlayerCharacter currentPlayer = FindFirstObjectByType<PlayerCharacter>();
        if (SceneManager.GetActiveScene().name == SceneNames.Battle)
            ExplorationPlayerSession.SaveBattle(currentPlayer, FindFirstObjectByType<EnemyCharacter>());
        else
            ExplorationPlayerSession.SavePlayer(currentPlayer);
    }

    private void ResetPauseMenu()
    {
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    private static void ClearSelectedButton()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
