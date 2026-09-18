using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
    [SerializeField] private GameObject inventoryEmptyState;
    [SerializeField] private GameObject deckEmptyState;

    [SerializeField] private TMP_Text inventoryCountText;
    [SerializeField] private TMP_Text deckCountText;

    [SerializeField] private InventoryPreviewPanel inventoryPreviewPanel;
    [SerializeField] private InventoryCategoriesPanel categoriesPanel;

    [Header("Top Icons")]
    [SerializeField] private List<GameObject> topIconBorders = new();
    [SerializeField] private List<GameObject> pages = new();
    [SerializeField] private TMP_Text goldBalanceText;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private RectTransform saveToastRect;

    private QuantitySelectionPrompt transferQuantityPrompt;

    private static readonly GameInputAction[] SettingsActions =
    {
        GameInputAction.MoveUp,
        GameInputAction.MoveDown,
        GameInputAction.MoveLeft,
        GameInputAction.MoveRight,
        GameInputAction.Interact,
        GameInputAction.Inventory,
        GameInputAction.EndTurn,
        GameInputAction.Close
    };

    private static readonly Color OverlayColor = new(0.1f, 0.08f, 0.07f, 0.74f);
    private static readonly Color PanelColor = new(0.31f, 0.23f, 0.2f, 1f);
    private static readonly Color ButtonColor = new(0.66f, 0.59f, 0.55f, 1f);
    private static readonly Color ButtonHighlightedColor = new(0.82f, 0.75f, 0.7f, 1f);
    private static readonly Color ButtonPressedColor = new(0.56f, 0.49f, 0.45f, 1f);
    private static readonly Color ConfirmButtonColor = new(0.48f, 0.37f, 0.32f, 1f);
    private static readonly Color ConfirmButtonHighlightedColor = new(0.58f, 0.46f, 0.4f, 1f);
    private static readonly Color ConfirmButtonPressedColor = new(0.38f, 0.28f, 0.24f, 1f);
    private static readonly Color SettingsRowColor = new(0.39f, 0.29f, 0.25f, 0.94f);
    private static readonly Color SettingsRowHighlightedColor = new(0.49f, 0.38f, 0.33f, 1f);
    private static readonly Color SettingsRowPressedColor = new(0.31f, 0.23f, 0.2f, 1f);
    private static readonly Color SettingsActionButtonColor = new(0.55f, 0.43f, 0.37f, 1f);
    private static readonly Color SettingsActionButtonHighlightedColor = new(0.65f, 0.53f, 0.47f, 1f);
    private static readonly Color SettingsActionButtonPressedColor = new(0.45f, 0.34f, 0.29f, 1f);
    private static readonly Color TextColor = new(0.96f, 0.945f, 0.91f, 1f);
    private static readonly Color SaveToastColor = new(0.22f, 0.62f, 0.34f, 0.98f);
    private static readonly Vector2 SaveToastVisiblePosition = new(-34f, -34f);
    private static readonly Vector2 SaveToastHiddenPosition = new(330f, -34f);
    private const float SaveToastSlideDuration = 0.22f;
    private const float SaveToastHoldDuration = 1.25f;

    private InventoryScreenController inventoryScreenController;
    private Coroutine saveToastCoroutine;
    private TMP_Text confirmationTitleText;
    private PendingPauseAction pendingPauseAction;
    private int selectedTopIconIndex = -1;

    public bool IsOpen => gameObject.activeSelf;

    private enum PendingPauseAction
    {
        None,
        ReturnToMainMenu,
        QuitGame
    }

    private void Awake()
    {
        EnsurePauseMenuReferences();
        EnsurePauseMenuRuntimeFallbacks();
        EnsureTransferQuantityPrompt();

        if (!TryInitializeInventory())
            Debug.LogError("PlayerCharacter is missing. InventoryWindow could not initialize.");

        ResetPauseMenu();
    }

    private bool TryInitializeInventory()
    {
        if (inventoryScreenController != null)
            return true;

        player ??= FindFirstObjectByType<PlayerCharacter>(FindObjectsInactive.Include);
        if (player == null || player.InventoryData == null || player.DeckData == null)
            return false;

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
            deckDrawer,
            ShowTransferQuantityPrompt,
            visible => SetEmptyStateVisible(inventoryEmptyState, visible),
            visible => SetEmptyStateVisible(deckEmptyState, visible));

        InventoryViewUtils.Cleanup(
            inventoryCardsContent,
            deckCardsContent);

        return true;
    }

    private void Update()
    {
        if (IsOpen && GameInputSettings.GetKeyDown(GameInputAction.Close))
            HandleCloseInput();
    }

    private void OnEnable()
    {
        PlayerWallet.GoldChanged += HandleGoldChanged;
        RefreshGoldBalance();

        if (!TryInitializeInventory())
            return;

        if (playerHPText != null)
            playerHPText.SetTarget(player);

        RebuildCategories();

        inventoryPreviewPanel.Clear();
        
        SelectTopIcon(0);
        Refresh();
    }

    private void OnDisable()
    {
        PlayerWallet.GoldChanged -= HandleGoldChanged;
        HideTransferQuantityPrompt();

        if (GameStateManager.State == GameState.Inventory)
            GameStateManager.SetState(GameState.Gameplay);
    }

    private void HandleGoldChanged(int _)
    {
        RefreshGoldBalance();
    }

    private void RefreshGoldBalance()
    {
        if (goldBalanceText != null)
            goldBalanceText.text = PlayerWallet.Gold.ToString();
    }

    public void Refresh()
    {
        if (inventoryScreenController == null)
            return;

        inventoryScreenController.Refresh(
            inventoryCountText,
            deckCountText);
    }

    public void RebuildCategoriesAndRefresh()
    {
        if (inventoryScreenController == null)
            return;

        if (categoriesPanel != null && player != null && player.InventoryData != null)
            RebuildCategories();

        Refresh();
    }

    public void AddSelectedToDeck()
    {
        if (!GameStateManager.CanEditCardCollections())
            return;

        inventoryScreenController.AddSelected();
    }

    public void RemoveSelectedFromDeck()
    {
        if (!GameStateManager.CanEditCardCollections())
            return;

        inventoryScreenController.RemoveSelected();
    }

    public void Open()
    {
        if (!GameStateManager.CanOpenInventory())
            return;

        GameStateManager.SetState(GameState.Inventory);
        gameObject.SetActive(true);

        if (!TryInitializeInventory())
        {
            Debug.LogError("InventoryWindow cannot open because PlayerCharacter is missing.");
            GameStateManager.SetState(GameState.Gameplay);
            return;
        }

        inventoryScreenController.ClearSelection();

        inventoryPreviewPanel.Clear();
        ResetPauseMenu();
        SelectTopIcon(0);
        Refresh();
    }

    public void Close()
    {
        HideTransferQuantityPrompt();
        inventoryScreenController?.ClearSelection();
        ResetPauseMenu();

        GameStateManager.SetState(GameState.Gameplay);
        gameObject.SetActive(false);
    }

    public void ResumeGame()
    {
        ClearSelectedButton();
        ResetPauseMenu();
        SelectTopIcon(0);
    }

    public void ShowOptions()
    {
        ClearSelectedButton();
        HideSaveToast();

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
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
        ShowSaveToast();
    }

    public void QuitGame()
    {
        ClearSelectedButton();
        ShowExitConfirmation(PendingPauseAction.QuitGame);
    }

    public void Toggle()
    {
        if (!GameStateManager.CanOpenInventory())
        {
            if (IsOpen)
                Close();

            return;
        }

        if (IsOpen)
            Close();
        else
            Open();
    }

    public void SelectTopIcon(int index)
    {
        if (inventoryScreenController == null || index < 0 || index >= topIconBorders.Count)
            return;

        selectedTopIconIndex = index;

        if (categoriesPanel != null && categoriesPanel.gameObject.activeSelf)
            categoriesPanel.ResetToAll();
        else
            inventoryScreenController.SetCategory(null);

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

    private void RebuildCategories()
    {
        if (categoriesPanel == null || player == null || player.InventoryData == null)
            return;

        bool hasCards = player.InventoryData.Cards != null && player.InventoryData.Cards.Count > 0;
        categoriesPanel.gameObject.SetActive(true);

        if (!hasCards)
        {
            categoriesPanel.ClearButtons();
            inventoryScreenController.SetCategory(null);
            return;
        }

        categoriesPanel.Build(
            player.InventoryData.Cards,
            category =>
            {
                inventoryScreenController.SetCategory(category);
                Refresh();
            });
    }

    private static void SetEmptyStateVisible(GameObject emptyState, bool visible)
    {
        if (emptyState == null)
            return;

        emptyState.SetActive(visible);
        if (visible)
            emptyState.transform.SetAsLastSibling();
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
        ClearSelectedButton();
        ShowExitConfirmation(PendingPauseAction.ReturnToMainMenu);
    }

    public void ConfirmExit()
    {
        ClearSelectedButton();

        PendingPauseAction action = pendingPauseAction;
        pendingPauseAction = PendingPauseAction.None;

        if (action == PendingPauseAction.ReturnToMainMenu)
        {
            MainMenuNavigation.GoToMainMenu();
            return;
        }

        if (action != PendingPauseAction.QuitGame)
            return;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CancelExit()
    {
        ClearSelectedButton();
        pendingPauseAction = PendingPauseAction.None;

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    private void EnsurePauseMenuReferences()
    {
        if (pauseOverlay == null)
            pauseOverlay = transform.Find("PauseOverlay")?.gameObject;
        if (pauseOverlay == null)
            return;

        if (pauseMenuPanel == null)
            pauseMenuPanel = pauseOverlay.transform.Find("PausePanel")?.gameObject;
        if (optionsPanel == null)
            optionsPanel = pauseOverlay.transform.Find("OptionsPanel")?.gameObject;
        if (confirmationPanel == null)
            confirmationPanel = pauseOverlay.transform.Find("ConfirmExitPanel")?.gameObject;
        if (confirmationTitleText == null && confirmationPanel != null)
            confirmationTitleText = confirmationPanel.transform.Find("ConfirmExitTitle")?.GetComponent<TMP_Text>();
        if (saveToastRect == null)
            saveToastRect = pauseOverlay.transform.Find("SaveToast") as RectTransform;
    }

    private void EnsurePauseMenuRuntimeFallbacks()
    {
        ApplyOverlayStyle();
        ApplyPanelStyle(pauseMenuPanel);
        ApplyPanelStyle(optionsPanel);
        ApplyExistingButtonStyles(pauseMenuPanel);
        ApplyExistingButtonStyles(optionsPanel);
        EnsureSaveToast();
        EnsureConfirmationPanel();
        EnsureSettingsPanel();
    }

    private void ApplyOverlayStyle()
    {
        if (pauseOverlay == null)
            return;

        Image overlayImage = pauseOverlay.GetComponent<Image>();
        if (overlayImage != null)
            overlayImage.color = OverlayColor;
    }

    private static void ApplyPanelStyle(GameObject panel)
    {
        if (panel == null)
            return;

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.color = PanelColor;
    }

    private static void ApplyExistingButtonStyles(GameObject panel)
    {
        if (panel == null)
            return;

        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
            ApplyButtonColors(button, ButtonColor, ButtonHighlightedColor, ButtonPressedColor);
    }

    private void EnsureSaveToast()
    {
        if (pauseOverlay == null)
            return;

        if (saveToastRect != null)
        {
            Image image = saveToastRect.GetComponent<Image>();
            if (image != null)
                image.color = SaveToastColor;

            HideSaveToast();
            return;
        }

        saveToastRect = CreateUIObject("SaveToast", pauseOverlay.transform);
        saveToastRect.anchorMin = new Vector2(1f, 1f);
        saveToastRect.anchorMax = new Vector2(1f, 1f);
        saveToastRect.pivot = new Vector2(1f, 1f);
        saveToastRect.sizeDelta = new Vector2(300f, 58f);
        saveToastRect.anchoredPosition = SaveToastHiddenPosition;

        Image toastImage = saveToastRect.gameObject.AddComponent<Image>();
        toastImage.color = SaveToastColor;

        CreateText("Text", saveToastRect, "Game saved", 26, Vector2.zero, new Vector2(270f, 46f), TextAlignmentOptions.Center);
        HideSaveToast();
    }

    private void EnsureTransferQuantityPrompt()
    {
        if (transferQuantityPrompt != null)
            return;

        TMP_Text referenceText = pauseMenuPanel != null
            ? pauseMenuPanel.GetComponentInChildren<TMP_Text>(true)
            : inventoryCountText;
        transferQuantityPrompt = QuantitySelectionPrompt.Create(GetOverlayParent(), referenceText);
    }

    private Transform GetOverlayParent()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.rootCanvas != null)
            return canvas.rootCanvas.transform;

        return transform;
    }

    private void ShowTransferQuantityPrompt(
        CardData card,
        InventoryCardLocation source,
        int maxQuantity,
        Action<int> onConfirmed)
    {
        EnsureTransferQuantityPrompt();
        string action = source == InventoryCardLocation.Inventory ? "Add to deck" : "Remove from deck";
        transferQuantityPrompt.Show($"{action}: {GetCardDisplayName(card)}", "Move", maxQuantity, onConfirmed);
        ClearSelectedButton();
    }

    private void HideTransferQuantityPrompt()
    {
        transferQuantityPrompt?.Hide();
    }

    private void HandleCloseInput()
    {
        ClearSelectedButton();

        if (transferQuantityPrompt != null && transferQuantityPrompt.IsOpen)
        {
            HideTransferQuantityPrompt();
            return;
        }

        if (confirmationPanel != null && confirmationPanel.activeSelf)
        {
            CancelExit();
            return;
        }

        if (optionsPanel != null && optionsPanel.activeSelf)
        {
            HideOptions();
            return;
        }

        if (pauseOverlay != null && pauseOverlay.activeSelf)
        {
            ResumeGame();
            return;
        }

        Close();
    }

    private static string GetCardDisplayName(CardData card)
    {
        if (card == null)
            return "Card";

        return string.IsNullOrWhiteSpace(card.CardName) ? card.name : card.CardName;
    }

    private void EnsureConfirmationPanel()
    {
        if (pauseOverlay == null)
            return;

        if (confirmationPanel != null)
        {
            StyleConfirmationButtons();
            confirmationPanel.SetActive(false);
            return;
        }

        confirmationPanel = CreatePanel("ConfirmExitPanel", pauseOverlay.transform, new Vector2(560f, 360f));
        confirmationTitleText = CreateText("ConfirmExitTitle", confirmationPanel.transform, "Exit?", 46, new Vector2(0f, 110f), new Vector2(480f, 62f), TextAlignmentOptions.Center);
        CreateText("ConfirmExitMessage", confirmationPanel.transform, "Unsaved changes will not be saved.", 28, new Vector2(0f, 38f), new Vector2(500f, 70f), TextAlignmentOptions.Center);

        CreateButton("ConfirmExitYesButton", confirmationPanel.transform, "Yes", new Vector2(-115f, -78f), new Vector2(190f, 56f), ConfirmExit, ConfirmButtonColor, ConfirmButtonHighlightedColor, ConfirmButtonPressedColor);
        CreateButton("ConfirmExitNoButton", confirmationPanel.transform, "No", new Vector2(115f, -78f), new Vector2(190f, 56f), CancelExit, ConfirmButtonColor, ConfirmButtonHighlightedColor, ConfirmButtonPressedColor);
        StyleConfirmationButtons();
        confirmationPanel.SetActive(false);
    }

    private void StyleConfirmationButtons()
    {
        if (confirmationPanel == null)
            return;

        Button[] buttons = confirmationPanel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
            ApplyButtonColors(button, ConfirmButtonColor, ConfirmButtonHighlightedColor, ConfirmButtonPressedColor);
    }

    private void EnsureSettingsPanel()
    {
        if (optionsPanel == null)
            return;

        TMP_Text title = optionsPanel.transform.Find("OptionsTitle")?.GetComponent<TMP_Text>();
        RectTransform optionsRect = optionsPanel.GetComponent<RectTransform>();
        if (optionsRect != null)
            optionsRect.sizeDelta = new Vector2(680f, 640f);

        if (title != null)
        {
            title.text = "Settings";
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(470f, 70f);
            titleRect.anchoredPosition = new Vector2(0f, 245f);
            title.fontSize = 42f;
        }

        if (optionsPanel.transform.Find("SettingsBody") != null)
        {
            RectTransform existingBodyRect = optionsPanel.transform.Find("SettingsBody") as RectTransform;
            Transform content = optionsPanel.transform.Find("SettingsBody/BindingsContent");
            Transform controlsTitle = optionsPanel.transform.Find("ControlsTitle");
            Button existingResetButton = optionsPanel.transform.Find("SettingsResetButton")?.GetComponent<Button>();
            EnsureSettingsScrollbar(existingBodyRect, existingBodyRect != null ? existingBodyRect.GetComponent<ScrollRect>() : null);
            SettingsTabsPanel.Ensure(optionsPanel.transform, content, existingResetButton, controlsTitle);
            AlignSettingsActionButtons();
            StyleSettingsButtons(optionsPanel.transform);
            return;
        }

        CreateText("ControlsTitle", optionsPanel.transform, "Controls", 28, new Vector2(0f, 146f), new Vector2(470f, 34f), TextAlignmentOptions.Center);

        RectTransform bodyRect = CreateUIObject("SettingsBody", optionsPanel.transform);
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(540f, 310f);
        bodyRect.anchoredPosition = new Vector2(0f, -32f);

        Image bodyImage = bodyRect.gameObject.AddComponent<Image>();
        bodyImage.color = Color.clear;
        bodyRect.gameObject.AddComponent<RectMask2D>();

        ScrollRect scrollRect = bodyRect.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 18f;
        scrollRect.viewport = bodyRect;

        RectTransform contentRect = CreateUIObject("BindingsContent", bodyRect);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 360f);
        contentRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layoutGroup = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(20, 38, 0, 0);
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 7f;

        ContentSizeFitter contentSizeFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRect;

        foreach (GameInputAction action in SettingsActions)
            CreateSettingsBindingRow(contentRect.transform, action);

        EnsureSettingsScrollbar(bodyRect, scrollRect);
        Button resetButton = CreateButton("SettingsResetButton", optionsPanel.transform, "Reset", new Vector2(-145f, -250f), new Vector2(250f, 50f), GameInputSettings.ResetToDefaults, SettingsActionButtonColor, SettingsActionButtonHighlightedColor, SettingsActionButtonPressedColor);

        Transform backButton = optionsPanel.transform.Find("OptionsBackButton");
        if (backButton != null)
        {
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            if (backRect != null)
            {
                backRect.sizeDelta = new Vector2(250f, 50f);
                backRect.anchoredPosition = new Vector2(145f, -250f);
            }

            Button backUiButton = backButton.GetComponent<Button>();
            if (backUiButton != null)
                ApplyButtonColors(backUiButton, SettingsActionButtonColor, SettingsActionButtonHighlightedColor, SettingsActionButtonPressedColor);

            backButton.SetAsLastSibling();
        }

        resetButton.transform.SetSiblingIndex(backButton != null ? backButton.GetSiblingIndex() : resetButton.transform.GetSiblingIndex());
        SettingsTabsPanel.Ensure(optionsPanel.transform, contentRect.transform, resetButton, optionsPanel.transform.Find("ControlsTitle"));
    }

    private void AlignSettingsActionButtons()
    {
        if (optionsPanel == null)
            return;

        AlignSettingsActionButton(optionsPanel.transform.Find("SettingsResetButton") as RectTransform, new Vector2(-145f, -250f));
        AlignSettingsActionButton(optionsPanel.transform.Find("OptionsBackButton") as RectTransform, new Vector2(145f, -250f));
    }

    private static void AlignSettingsActionButton(RectTransform rect, Vector2 position)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(250f, 50f);
        rect.anchoredPosition = position;
    }

    private static void StyleSettingsButtons(Transform optionsTransform)
    {
        foreach (Button button in optionsTransform.GetComponentsInChildren<Button>(true))
        {
            if (button.name.StartsWith("BindingButton_"))
                ApplyButtonColors(button, SettingsRowColor, SettingsRowHighlightedColor, SettingsRowPressedColor);
            else if (button.name == "SettingsResetButton" || button.name == "OptionsBackButton")
                ApplyButtonColors(button, SettingsActionButtonColor, SettingsActionButtonHighlightedColor, SettingsActionButtonPressedColor);
        }
    }

    private void CreateSettingsBindingRow(Transform parent, GameInputAction action)
    {
        Button rowButton = CreateButton($"BindingButton_{action}", parent, string.Empty, Vector2.zero, new Vector2(500f, 38f), null, SettingsRowColor, SettingsRowHighlightedColor, SettingsRowPressedColor);
        rowButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

        TMP_Text rowText = CreateText("Label", rowButton.transform, string.Empty, 24, Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
        RectTransform rowTextRect = rowText.GetComponent<RectTransform>();
        rowTextRect.anchorMin = Vector2.zero;
        rowTextRect.anchorMax = Vector2.one;
        rowTextRect.sizeDelta = new Vector2(-24f, 0f);
        rowTextRect.anchoredPosition = Vector2.zero;

        SettingsKeyBindingRow bindingRow = rowButton.gameObject.AddComponent<SettingsKeyBindingRow>();
        bindingRow.Configure(action, null, rowText, rowButton);
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        RectTransform rectTransform = CreateUIObject(name, parent);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = Vector2.zero;

        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = PanelColor;

        return rectTransform.gameObject;
    }

    private void EnsureSettingsScrollbar(RectTransform bodyRect, ScrollRect scrollRect)
    {
        if (bodyRect == null || scrollRect == null)
            return;

        Scrollbar scrollbar = bodyRect.Find("BindingsScrollbar")?.GetComponent<Scrollbar>();
        if (scrollbar == null)
            scrollbar = CreateSettingsScrollbar(bodyRect);

        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalScrollbarSpacing = 0f;
        scrollbar.gameObject.SetActive(false);
    }

    private Scrollbar CreateSettingsScrollbar(RectTransform bodyRect)
    {
        RectTransform scrollbarRect = CreateUIObject("BindingsScrollbar", bodyRect);
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 1f);
        scrollbarRect.sizeDelta = new Vector2(14f, 0f);
        scrollbarRect.anchoredPosition = Vector2.zero;

        Image scrollbarImage = scrollbarRect.gameObject.AddComponent<Image>();
        scrollbarImage.color = new Color(0.22f, 0.16f, 0.14f, 0.95f);

        Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        RectTransform slidingArea = CreateUIObject("Sliding Area", scrollbarRect);
        slidingArea.anchorMin = Vector2.zero;
        slidingArea.anchorMax = Vector2.one;
        slidingArea.pivot = new Vector2(0.5f, 0.5f);
        slidingArea.sizeDelta = Vector2.zero;

        RectTransform handleRect = CreateUIObject("Handle", slidingArea);
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = Vector2.zero;

        Image handleImage = handleRect.gameObject.AddComponent<Image>();
        handleImage.color = ButtonColor;

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        scrollbar.value = 1f;
        return scrollbar;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick, Color normalColor, Color highlightedColor, Color pressedColor)
    {
        RectTransform rectTransform = CreateUIObject(name, parent);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = position;

        rectTransform.gameObject.AddComponent<Image>();

        Button button = rectTransform.gameObject.AddComponent<Button>();
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        ApplyButtonColors(button, normalColor, highlightedColor, pressedColor);

        if (onClick != null)
            button.onClick.AddListener(onClick);

        if (!string.IsNullOrEmpty(label))
            CreateText("Text", rectTransform, label, 26, Vector2.zero, size, TextAlignmentOptions.Center);

        return button;
    }

    private static void ApplyButtonColors(Button button, Color normalColor, Color highlightedColor, Color pressedColor)
    {
        if (button.targetGraphic is Image image)
            image.color = normalColor;

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.selectedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        button.colors = colors;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, float fontSize, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        RectTransform rectTransform = CreateUIObject(name, parent);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = position;

        TextMeshProUGUI label = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_Text referenceText = pauseMenuPanel != null
            ? pauseMenuPanel.GetComponentInChildren<TMP_Text>(true)
            : null;

        if (referenceText != null)
        {
            label.font = referenceText.font;
            label.fontMaterial = referenceText.fontMaterial;
        }

        label.text = text;
        label.fontSize = fontSize;
        label.color = TextColor;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        return label;
    }

    private static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);

        return gameObject.GetComponent<RectTransform>();
    }

    private void ShowExitConfirmation(PendingPauseAction action)
    {
        HideSaveToast();
        pendingPauseAction = action;
        UpdateConfirmationTitle(action);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    private void UpdateConfirmationTitle(PendingPauseAction action)
    {
        if (confirmationTitleText == null && confirmationPanel != null)
            confirmationTitleText = confirmationPanel.transform.Find("ConfirmExitTitle")?.GetComponent<TMP_Text>();
        if (confirmationTitleText == null)
            return;

        confirmationTitleText.text = action == PendingPauseAction.ReturnToMainMenu
            ? "Back to menu?"
            : "Quit game?";
    }

    private void ShowSaveToast()
    {
        if (saveToastRect == null)
            return;

        if (saveToastCoroutine != null)
            StopCoroutine(saveToastCoroutine);

        saveToastRect.gameObject.SetActive(true);
        saveToastCoroutine = StartCoroutine(AnimateSaveToast());
    }

    private IEnumerator AnimateSaveToast()
    {
        yield return SlideSaveToast(SaveToastHiddenPosition, SaveToastVisiblePosition);
        yield return new WaitForSecondsRealtime(SaveToastHoldDuration);
        yield return SlideSaveToast(SaveToastVisiblePosition, SaveToastHiddenPosition);

        saveToastRect.gameObject.SetActive(false);
        saveToastCoroutine = null;
    }

    private IEnumerator SlideSaveToast(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        saveToastRect.anchoredPosition = from;

        while (elapsed < SaveToastSlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / SaveToastSlideDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            saveToastRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        saveToastRect.anchoredPosition = to;
    }

    private void HideSaveToast()
    {
        if (saveToastCoroutine != null)
        {
            StopCoroutine(saveToastCoroutine);
            saveToastCoroutine = null;
        }

        if (saveToastRect != null)
        {
            saveToastRect.gameObject.SetActive(false);
            saveToastRect.anchoredPosition = SaveToastHiddenPosition;
        }
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
        pendingPauseAction = PendingPauseAction.None;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        HideSaveToast();
    }

    private static void ClearSelectedButton()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
