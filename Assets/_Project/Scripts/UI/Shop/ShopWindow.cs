using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ShopWindow : MonoBehaviour
{
    private static ShopWindow instance;

    private static readonly Color ScreenColor = new(0.13f, 0.09f, 0.075f, 0.86f);
    private static readonly Color HeaderColor = new(0.33f, 0.22f, 0.19f, 1f);
    private static readonly Color PanelColor = new(0.46f, 0.36f, 0.33f, 0.98f);
    private static readonly Color PanelDarkColor = new(0.36f, 0.25f, 0.21f, 1f);
    private static readonly Color PanelSoftColor = new(0.58f, 0.49f, 0.46f, 1f);
    private static readonly Color RowColor = new(0.58f, 0.43f, 0.36f, 1f);
    private static readonly Color RowHighlightColor = new(0.68f, 0.52f, 0.43f, 1f);
    private static readonly Color RowPressedColor = new(0.5f, 0.36f, 0.3f, 1f);
    private static readonly Color SelectedColor = new(0.68f, 0.43f, 0.19f, 1f);
    private static readonly Color TextColor = new(0.97f, 0.94f, 0.88f, 1f);
    private static readonly Color MutedTextColor = new(0.78f, 0.7f, 0.64f, 1f);
    private static readonly Color GoldColor = new(1f, 0.74f, 0.27f, 1f);
    private static readonly Color DisabledColor = new(0.4f, 0.34f, 0.31f, 0.9f);
    private static readonly Color BuyButtonColor = new(0.58f, 0.42f, 0.24f, 1f);
    private static readonly Color BuyButtonHighlightColor = new(0.71f, 0.52f, 0.29f, 1f);
    private static readonly Color BuyButtonPressedColor = new(0.44f, 0.31f, 0.18f, 1f);
    private static readonly Color ScrollbarTrackColor = new(0.27f, 0.18f, 0.15f, 0.95f);
    private static readonly Color ScrollbarHandleColor = new(0.52f, 0.34f, 0.24f, 1f);
    private static readonly Color ScrollbarHandleHighlightColor = new(0.64f, 0.43f, 0.31f, 1f);
    private static readonly Color ScrollbarHandlePressedColor = new(0.42f, 0.27f, 0.2f, 1f);

    private ShopData activeShop;
    private NpcCharacter activeNpc;
    private PlayerCharacter activePlayer;
    private InventoryData activeInventory;
    private GameState previousState = GameState.Gameplay;

    private TMP_Text titleText;
    private TMP_Text goldText;
    private TMP_Text detailsNameText;
    private TMP_Text detailsDescriptionText;
    private TMP_Text detailsMetaText;
    private TMP_Text statusText;
    private TMP_Text buyButtonText;
    private Image detailsIcon;
    private Button buyButton;
    private Transform inventoryContent;
    private Transform shopContent;
    private readonly List<ShopRowView> inventoryRows = new();
    private readonly List<ShopRowView> shopRows = new();

    private int selectedShopIndex = -1;
    private CardData selectedInventoryCard;
    private bool selectedFromShop = true;

    private sealed class ShopRowView
    {
        private readonly Image background;
        private readonly Button button;
        private readonly TMP_Text subtitleText;
        private readonly TMP_Text rightText;
        private bool available;

        public ShopRowView(Image background, Button button, TMP_Text subtitleText, TMP_Text rightText, bool available)
        {
            this.background = background;
            this.button = button;
            this.subtitleText = subtitleText;
            this.rightText = rightText;
            this.available = available;
        }

        public void SetSubtitle(string text)
        {
            if (subtitleText != null)
                subtitleText.text = text;
        }

        public void SetRightText(string text)
        {
            if (rightText != null)
                rightText.text = text;
        }

        public void SetAvailable(bool value)
        {
            available = value;
        }

        public void SetSelected(bool selected)
        {
            Color normal = selected ? SelectedColor : available ? RowColor : DisabledColor;
            if (background != null)
                background.color = normal;

            if (button != null)
                button.colors = CreateButtonColors(normal, RowHighlightColor, RowPressedColor);
        }
    }

    public static bool Open(ShopData shop, NpcCharacter npc = null, PlayerCharacter player = null)
    {
        if (shop == null || !shop.HasItems)
            return false;

        ShopWindow window = EnsureInstance();
        if (window == null)
            return false;

        player ??= FindFirstObjectByType<PlayerCharacter>(FindObjectsInactive.Include);
        if (player == null || player.InventoryData == null)
            return false;

        window.Show(shop, npc, player);
        return true;
    }

    private static ShopWindow EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<ShopWindow>(FindObjectsInactive.Include);
        if (instance != null)
            return instance;

        GameObject root = new("ShopWindow");
        instance = root.AddComponent<ShopWindow>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildUi();
        EnsureEventSystem();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerWallet.GoldChanged += HandleGoldChanged;
        RefreshGold();
    }

    private void OnDisable()
    {
        PlayerWallet.GoldChanged -= HandleGoldChanged;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (GameInputSettings.GetKeyDown(GameInputAction.Close))
            Close();
    }

    public void Close()
    {
        if (!gameObject.activeSelf)
            return;

        activeNpc?.EndInteraction();
        activeNpc = null;
        activeShop = null;
        activePlayer = null;
        activeInventory = null;
        selectedShopIndex = -1;
        selectedInventoryCard = null;

        if (GameStateManager.State == GameState.Shop)
        {
            GameState stateToRestore = previousState == GameState.Shop
                ? GameState.Gameplay
                : previousState;
            GameStateManager.SetState(stateToRestore);
        }

        gameObject.SetActive(false);
    }

    private void Show(ShopData shop, NpcCharacter npc, PlayerCharacter player)
    {
        activeShop = shop;
        activeNpc = npc;
        activePlayer = player;
        activeInventory = player.InventoryData;
        selectedInventoryCard = null;
        selectedFromShop = true;
        selectedShopIndex = FindFirstAvailableShopIndex();
        statusText.text = string.Empty;

        if (player.Data != null)
            PlayerWallet.Initialize(player.Data.startingGold);

        if (activeNpc != null && activeNpc.State != NpcState.Trading)
            activeNpc.TryInteract(NpcInteractionType.Trade);

        previousState = GameStateManager.State == GameState.Shop
            ? GameState.Gameplay
            : GameStateManager.State;

        GameStateManager.SetState(GameState.Shop);
        gameObject.SetActive(true);

        titleText.text = activeShop.DisplayName;
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshGold();
        RefreshInventory();
        RefreshShop();
        RefreshDetails();
    }

    private void RefreshGold()
    {
        if (goldText != null)
            goldText.text = $"Gold {PlayerWallet.Gold}";
    }

    private void HandleGoldChanged(int _)
    {
        RefreshGold();
        RefreshDetails();
    }

    private void RefreshInventory()
    {
        ClearChildren(inventoryContent);
        inventoryRows.Clear();

        if (activeInventory == null || activeInventory.Cards == null || activeInventory.Cards.Count == 0)
        {
            CreateEmptyText(inventoryContent, "Inventory is empty.");
            return;
        }

        Dictionary<CardData, int> counts = activeInventory.Cards
            .Where(card => card != null)
            .GroupBy(card => card)
            .ToDictionary(group => group.Key, group => group.Count());

        List<CardData> cards = counts.Keys
            .OrderBy(GetCardName)
            .ToList();

        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            int count = counts[card];
            bool selected = !selectedFromShop && selectedInventoryCard == card;
            ShopRowView row = CreateItemRow(
                inventoryContent,
                card,
                GetCardName(card),
                card.ShortDescription,
                $"x{count}",
                selected,
                true,
                () => SelectInventoryCard(card));
            inventoryRows.Add(row);
        }
    }

    private void RefreshShop()
    {
        ClearChildren(shopContent);
        shopRows.Clear();

        if (activeShop == null || activeShop.Items == null || activeShop.Items.Count == 0)
        {
            CreateEmptyText(shopContent, "Nothing for sale.");
            return;
        }

        bool hasAny = false;
        for (int i = 0; i < activeShop.Items.Count; i++)
        {
            ShopItemData item = activeShop.Items[i];
            if (item == null || !item.IsValid)
                continue;

            hasAny = true;
            int index = i;
            int remainingStock = ShopManager.GetRemainingStock(activeShop, index);
            int buyPrice = ShopManager.GetBuyPrice(activeShop, index);
            bool available = remainingStock > 0;
            bool selected = selectedFromShop && selectedShopIndex == index;
            string stockText = item.HasLimitedStock
                ? available ? $"Stock {remainingStock}" : "Sold out"
                : "Unlimited";

            ShopRowView row = CreateItemRow(
                shopContent,
                item.card,
                GetCardName(item.card),
                stockText,
                $"{buyPrice} gold",
                selected,
                available,
                () => SelectShopItem(index));
            shopRows.Add(row);
        }

        if (!hasAny)
            CreateEmptyText(shopContent, "Nothing for sale.");
    }

    private void RefreshDetails()
    {
        if (selectedFromShop)
        {
            RefreshShopDetails();
            return;
        }

        RefreshInventoryDetails();
    }

    private void RefreshShopDetails()
    {
        if (!TryGetSelectedShopItem(out ShopItemData item))
        {
            ShowEmptyDetails("Select an item to buy.");
            return;
        }

        CardData card = item.card;
        ApplyDetailsCard(card);

        int remainingStock = ShopManager.GetRemainingStock(activeShop, selectedShopIndex);
        int buyPrice = ShopManager.GetBuyPrice(activeShop, selectedShopIndex);
        string stockText = item.HasLimitedStock
            ? remainingStock > 0 ? remainingStock.ToString() : "Sold out"
            : "Unlimited";

        detailsMetaText.text = $"Price: {buyPrice} gold\nStock: {stockText}";
        buyButton.gameObject.SetActive(true);
        buyButton.interactable = remainingStock > 0 && PlayerWallet.Gold >= buyPrice;
        buyButtonText.text = remainingStock > 0 ? "Buy" : "Sold out";
    }

    private void RefreshInventoryDetails()
    {
        if (selectedInventoryCard == null)
        {
            ShowEmptyDetails("Select an item.");
            return;
        }

        ApplyDetailsCard(selectedInventoryCard);
        int ownedCount = activeInventory != null && activeInventory.Cards != null
            ? activeInventory.Cards.Count(card => card == selectedInventoryCard)
            : 0;

        int sellPrice = ShopManager.GetSellPrice(activeShop, selectedInventoryCard);
        detailsMetaText.text = $"Owned: x{ownedCount}\nSell price: {sellPrice} gold";
        buyButton.gameObject.SetActive(true);
        buyButton.interactable = ownedCount > 0 && sellPrice > 0;
        buyButtonText.text = "Sell";
    }

    private void ApplyDetailsCard(CardData card)
    {
        detailsNameText.text = GetCardName(card);
        detailsDescriptionText.text = card != null ? card.Description : string.Empty;

        if (detailsIcon != null)
        {
            detailsIcon.sprite = card != null ? card.Icon : null;
            detailsIcon.enabled = card != null && card.Icon != null;
            detailsIcon.preserveAspect = true;
        }
    }

    private void ShowEmptyDetails(string text)
    {
        detailsNameText.text = text;
        detailsDescriptionText.text = string.Empty;
        detailsMetaText.text = string.Empty;
        detailsIcon.sprite = null;
        detailsIcon.enabled = false;
        buyButton.gameObject.SetActive(true);
        buyButton.interactable = false;
        buyButtonText.text = "Select";
    }

    private void SelectShopItem(int itemIndex)
    {
        selectedShopIndex = itemIndex;
        selectedInventoryCard = null;
        selectedFromShop = true;
        statusText.text = string.Empty;
        ClearSelectedButton();
        RefreshRowSelectionStates();
        RefreshDetails();
    }

    private void SelectInventoryCard(CardData card)
    {
        selectedInventoryCard = card;
        selectedFromShop = false;
        statusText.text = string.Empty;
        ClearSelectedButton();
        RefreshRowSelectionStates();
        RefreshDetails();
    }

    private void BuySelected()
    {
        ClearSelectedButton();

        if (!selectedFromShop)
        {
            SellSelected();
            return;
        }

        if (!TryGetSelectedShopItem(out ShopItemData item))
            return;

        if (ShopManager.GetRemainingStock(activeShop, selectedShopIndex) <= 0)
        {
            statusText.text = "Sold out.";
            RefreshShopRowsData();
            RefreshDetails();
            return;
        }

        int buyPrice = ShopManager.GetBuyPrice(activeShop, selectedShopIndex);
        if (PlayerWallet.Gold < buyPrice)
        {
            statusText.text = "Not enough gold.";
            RefreshDetails();
            return;
        }

        bool bought = ShopManager.Buy(activeShop, selectedShopIndex, activeInventory);
        statusText.text = bought ? "Purchased." : "Can't buy this item.";

        if (bought && ShopManager.GetRemainingStock(activeShop, selectedShopIndex) <= 0)
            selectedShopIndex = FindFirstAvailableShopIndex();

        RefreshAfterTransaction();
    }

    private void RefreshRowSelectionStates()
    {
        if (activeInventory != null && activeInventory.Cards != null)
        {
            Dictionary<CardData, int> counts = activeInventory.Cards
                .Where(card => card != null)
                .GroupBy(card => card)
                .ToDictionary(group => group.Key, group => group.Count());

            List<CardData> cards = counts.Keys
                .OrderBy(GetCardName)
                .ToList();

            for (int i = 0; i < inventoryRows.Count && i < cards.Count; i++)
                inventoryRows[i].SetSelected(!selectedFromShop && selectedInventoryCard == cards[i]);
        }

        int rowIndex = 0;
        if (activeShop?.Items != null)
        {
            for (int i = 0; i < activeShop.Items.Count && rowIndex < shopRows.Count; i++)
            {
                ShopItemData item = activeShop.Items[i];
                if (item == null || !item.IsValid)
                    continue;

                shopRows[rowIndex].SetSelected(selectedFromShop && selectedShopIndex == i);
                rowIndex++;
            }
        }
    }

    private void RefreshAfterTransaction()
    {
        TryRefreshInventoryRowsData();
        RefreshShopRowsData();
        RefreshRowSelectionStates();
        RefreshDetails();
    }

    private bool TryRefreshInventoryRowsData()
    {
        if (activeInventory == null || activeInventory.Cards == null || activeInventory.Cards.Count == 0)
        {
            RefreshInventory();
            return false;
        }

        Dictionary<CardData, int> counts = activeInventory.Cards
            .Where(card => card != null)
            .GroupBy(card => card)
            .ToDictionary(group => group.Key, group => group.Count());

        List<CardData> cards = counts.Keys
            .OrderBy(GetCardName)
            .ToList();

        if (cards.Count != inventoryRows.Count)
        {
            RefreshInventory();
            return false;
        }

        for (int i = 0; i < inventoryRows.Count; i++)
            inventoryRows[i].SetRightText($"x{counts[cards[i]]}");

        return true;
    }

    private void RefreshShopRowsData()
    {
        int rowIndex = 0;
        if (activeShop?.Items == null)
            return;

        for (int i = 0; i < activeShop.Items.Count && rowIndex < shopRows.Count; i++)
        {
            ShopItemData item = activeShop.Items[i];
            if (item == null || !item.IsValid)
                continue;

            int remainingStock = ShopManager.GetRemainingStock(activeShop, i);
            bool available = remainingStock > 0;
            int buyPrice = ShopManager.GetBuyPrice(activeShop, i);
            string stockText = item.HasLimitedStock
                ? available ? $"Stock {remainingStock}" : "Sold out"
                : "Unlimited";

            shopRows[rowIndex].SetSubtitle(stockText);
            shopRows[rowIndex].SetRightText($"{buyPrice} gold");
            shopRows[rowIndex].SetAvailable(available);
            rowIndex++;
        }
    }

    private void SellSelected()
    {
        if (selectedInventoryCard == null || activeInventory == null)
            return;

        int sellPrice = ShopManager.GetSellPrice(activeShop, selectedInventoryCard);
        bool sold = ShopManager.Sell(activeShop, selectedInventoryCard, activeInventory);
        statusText.text = sold ? $"Sold for {sellPrice} gold." : "Can't sell this item.";

        if (activeInventory.Cards == null || !activeInventory.Cards.Contains(selectedInventoryCard))
            selectedInventoryCard = null;

        RefreshAfterTransaction();
    }

    private int FindFirstAvailableShopIndex()
    {
        if (activeShop == null || activeShop.Items == null)
            return -1;

        for (int i = 0; i < activeShop.Items.Count; i++)
        {
            ShopItemData item = activeShop.Items[i];
            if (item != null && item.IsValid && ShopManager.GetRemainingStock(activeShop, i) > 0)
                return i;
        }

        for (int i = 0; i < activeShop.Items.Count; i++)
        {
            ShopItemData item = activeShop.Items[i];
            if (item != null && item.IsValid)
                return i;
        }

        return -1;
    }

    private bool TryGetSelectedShopItem(out ShopItemData item)
    {
        item = null;
        if (activeShop == null || activeShop.Items == null || selectedShopIndex < 0 || selectedShopIndex >= activeShop.Items.Count)
            return false;

        item = activeShop.Items[selectedShopIndex];
        return item != null && item.IsValid;
    }

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 920;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform root = CreateRect("Root", transform);
        Stretch(root);
        AddImage(root, ScreenColor);

        RectTransform topBar = CreateRect("TopBar", root);
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.offsetMin = new Vector2(0f, -104f);
        topBar.offsetMax = Vector2.zero;
        AddImage(topBar, HeaderColor);

        Button closeButton = CreateButton("CloseButton", topBar, "X", 58f, HeaderColor, RowHighlightColor, RowPressedColor);
        RectTransform closeRect = closeButton.transform as RectTransform;
        closeRect.anchorMin = new Vector2(0f, 0.5f);
        closeRect.anchorMax = new Vector2(0f, 0.5f);
        closeRect.pivot = new Vector2(0.5f, 0.5f);
        closeRect.sizeDelta = new Vector2(92f, 92f);
        closeRect.anchoredPosition = new Vector2(66f, 0f);
        closeButton.onClick.AddListener(Close);

        titleText = CreateText("TitleText", topBar, "Trader", 52f, TextColor, TextAlignmentOptions.Center);
        RectTransform titleRect = titleText.transform as RectTransform;
        titleRect.anchorMin = new Vector2(0.18f, 0f);
        titleRect.anchorMax = new Vector2(0.82f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        goldText = CreateText("GoldText", topBar, "Gold 0", 42f, GoldColor, TextAlignmentOptions.MidlineRight);
        RectTransform goldRect = goldText.transform as RectTransform;
        goldRect.anchorMin = new Vector2(0.72f, 0f);
        goldRect.anchorMax = new Vector2(1f, 1f);
        goldRect.offsetMin = new Vector2(0f, 0f);
        goldRect.offsetMax = new Vector2(-44f, 0f);

        RectTransform body = CreateRect("Body", root);
        body.anchorMin = Vector2.zero;
        body.anchorMax = Vector2.one;
        body.offsetMin = new Vector2(70f, 58f);
        body.offsetMax = new Vector2(-70f, -144f);

        HorizontalLayoutGroup bodyLayout = body.gameObject.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 18f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = true;

        RectTransform inventoryPanel = CreateColumn(body, "Inventory", 520f, 1.05f);
        RectTransform detailsPanel = CreateColumn(body, "Details", 620f, 0.8f);
        RectTransform shopPanel = CreateColumn(body, "For Sale", 560f, 1.15f);

        inventoryContent = CreateScrollList(inventoryPanel, "InventoryScroll");
        BuildDetailsPanel(detailsPanel);
        shopContent = CreateScrollList(shopPanel, "ShopScroll");
    }

    private RectTransform CreateColumn(Transform parent, string header, float preferredWidth, float flexibleWidth)
    {
        RectTransform column = CreateRect($"{header}Column", parent);
        AddImage(column, PanelColor);

        LayoutElement layoutElement = column.gameObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = preferredWidth;
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.flexibleWidth = flexibleWidth;
        layoutElement.flexibleHeight = 1f;

        VerticalLayoutGroup layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 16, 18);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text headerText = CreateText($"{header}Header", column, header, 32f, TextColor, TextAlignmentOptions.MidlineLeft);
        LayoutElement headerLayout = headerText.gameObject.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 48f;

        return column;
    }

    private Transform CreateScrollList(Transform parent, string name)
    {
        RectTransform scrollRectTransform = CreateRect(name, parent);
        AddImage(scrollRectTransform, PanelSoftColor);
        LayoutElement layoutElement = scrollRectTransform.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleHeight = 1f;

        ScrollRect scrollRect = scrollRectTransform.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 42f;

        RectTransform viewport = CreateRect("Viewport", scrollRectTransform);
        Stretch(viewport);
        viewport.offsetMax = new Vector2(-24f, 0f);
        Image viewportImage = AddImage(viewport, new Color(0f, 0f, 0f, 0.01f));
        viewportImage.raycastTarget = true;
        Mask mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(10f, 0f);
        content.offsetMax = new Vector2(-10f, -10f);

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.verticalScrollbar = CreateScrollbar(scrollRectTransform);
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = 6f;
        return content;
    }

    private Scrollbar CreateScrollbar(Transform parent)
    {
        RectTransform scrollbarRect = CreateRect("Scrollbar", parent);
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(16f, 0f);
        scrollbarRect.anchoredPosition = new Vector2(-4f, 0f);

        AddImage(scrollbarRect, ScrollbarTrackColor);

        Scrollbar scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        RectTransform slidingArea = CreateRect("SlidingArea", scrollbarRect);
        Stretch(slidingArea);
        slidingArea.offsetMin = new Vector2(2f, 2f);
        slidingArea.offsetMax = new Vector2(-2f, -2f);

        RectTransform handle = CreateRect("Handle", slidingArea);
        Stretch(handle);
        Image handleImage = AddImage(handle, ScrollbarHandleColor);

        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handle;
        scrollbar.colors = CreateButtonColors(
            ScrollbarHandleColor,
            ScrollbarHandleHighlightColor,
            ScrollbarHandlePressedColor);
        return scrollbar;
    }

    private void BuildDetailsPanel(Transform detailsPanel)
    {
        RectTransform content = CreateRect("DetailsContent", detailsPanel);
        LayoutElement contentLayout = content.gameObject.AddComponent<LayoutElement>();
        contentLayout.flexibleHeight = 1f;
        contentLayout.minHeight = 0f;
        contentLayout.minWidth = 0f;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 14f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddImage(content, PanelSoftColor);

        RectTransform iconFrame = CreateRect("IconFrame", content);
        AddImage(iconFrame, PanelDarkColor);
        LayoutElement iconFrameLayout = iconFrame.gameObject.AddComponent<LayoutElement>();
        iconFrameLayout.minHeight = 260f;
        iconFrameLayout.preferredHeight = 260f;
        iconFrameLayout.flexibleHeight = 0f;

        detailsIcon = CreateImage("DetailsIcon", iconFrame, Color.white);
        RectTransform iconRect = detailsIcon.transform as RectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(190f, 190f);
        iconRect.anchoredPosition = Vector2.zero;

        detailsNameText = CreateText("DetailsNameText", content, "Select an item.", 34f, TextColor, TextAlignmentOptions.Center);
        LayoutElement nameLayout = detailsNameText.gameObject.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 52f;

        detailsDescriptionText = CreateText("DetailsDescriptionText", content, string.Empty, 24f, TextColor, TextAlignmentOptions.TopLeft);
        detailsDescriptionText.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement descriptionLayout = detailsDescriptionText.gameObject.AddComponent<LayoutElement>();
        descriptionLayout.preferredHeight = 150f;

        detailsMetaText = CreateText("DetailsMetaText", content, string.Empty, 26f, GoldColor, TextAlignmentOptions.TopLeft);
        detailsMetaText.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement metaLayout = detailsMetaText.gameObject.AddComponent<LayoutElement>();
        metaLayout.preferredHeight = 74f;

        statusText = CreateText("StatusText", content, string.Empty, 24f, GoldColor, TextAlignmentOptions.Center);
        LayoutElement statusLayout = statusText.gameObject.AddComponent<LayoutElement>();
        statusLayout.preferredHeight = 42f;

        RectTransform buttonSpacer = CreateRect("ButtonSpacer", content);
        LayoutElement spacerLayout = buttonSpacer.gameObject.AddComponent<LayoutElement>();
        spacerLayout.minHeight = 0f;
        spacerLayout.flexibleHeight = 10f;

        buyButton = CreateButton("BuyButton", content, "Buy", 30f, BuyButtonColor, BuyButtonHighlightColor, BuyButtonPressedColor);
        buyButtonText = buyButton.GetComponentInChildren<TMP_Text>();
        LayoutElement buyLayout = buyButton.gameObject.AddComponent<LayoutElement>();
        buyLayout.preferredHeight = 62f;
        buyButton.onClick.AddListener(BuySelected);
    }

    private ShopRowView CreateItemRow(
        Transform parent,
        CardData card,
        string title,
        string subtitle,
        string rightText,
        bool selected,
        bool available,
        Action onClick)
    {
        RectTransform row = CreateRect("ItemRow", parent);
        Image background = AddImage(row, selected ? SelectedColor : available ? RowColor : DisabledColor);
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 92f;

        Button button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.interactable = true;
        button.colors = CreateButtonColors(
            selected ? SelectedColor : available ? RowColor : DisabledColor,
            RowHighlightColor,
            RowPressedColor);
        button.onClick.AddListener(() => onClick?.Invoke());

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        Image icon = CreateImage("Icon", row, new Color(0.18f, 0.12f, 0.1f, 1f));
        icon.sprite = card != null ? card.Icon : null;
        icon.enabled = card != null && card.Icon != null;
        icon.color = icon.enabled ? Color.white : new Color(0.18f, 0.12f, 0.1f, 1f);
        icon.preserveAspect = false;
        LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.minWidth = 66f;
        iconLayout.preferredWidth = 66f;
        iconLayout.flexibleWidth = 0f;
        iconLayout.minHeight = 66f;
        iconLayout.preferredHeight = 66f;

        RectTransform textColumn = CreateRect("TextColumn", row);
        VerticalLayoutGroup textLayout = textColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 2f;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;
        LayoutElement textColumnLayout = textColumn.gameObject.AddComponent<LayoutElement>();
        textColumnLayout.minWidth = 0f;
        textColumnLayout.flexibleWidth = 1f;

        TMP_Text titleText = CreateText("Title", textColumn, title, 24f, available ? TextColor : MutedTextColor, TextAlignmentOptions.Left);
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Ellipsis;
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 34f;

        TMP_Text subtitleText = CreateText("Subtitle", textColumn, subtitle, 18f, available ? MutedTextColor : new Color(0.58f, 0.51f, 0.47f, 1f), TextAlignmentOptions.Left);
        subtitleText.textWrappingMode = TextWrappingModes.NoWrap;
        subtitleText.overflowMode = TextOverflowModes.Ellipsis;
        LayoutElement subtitleLayout = subtitleText.gameObject.AddComponent<LayoutElement>();
        subtitleLayout.preferredHeight = 28f;
        subtitleLayout.minWidth = 0f;
        subtitleLayout.flexibleWidth = 1f;

        TMP_Text right = CreateText("RightText", row, rightText, 22f, available ? GoldColor : MutedTextColor, TextAlignmentOptions.MidlineRight);
        right.textWrappingMode = TextWrappingModes.NoWrap;
        right.overflowMode = TextOverflowModes.Overflow;
        LayoutElement rightLayout = right.gameObject.AddComponent<LayoutElement>();
        rightLayout.minWidth = 86f;
        rightLayout.preferredWidth = 86f;
        rightLayout.flexibleWidth = 0f;

        return new ShopRowView(background, button, subtitleText, right, available);
    }

    private void CreateEmptyText(Transform parent, string text)
    {
        TMP_Text empty = CreateText("EmptyText", parent, text, 24f, MutedTextColor, TextAlignmentOptions.Center);
        empty.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement layout = empty.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 120f;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        RectTransform rectTransform = gameObject.transform as RectTransform;
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Image AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = AddImage(rect, color);
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        float fontSize,
        Color normal,
        Color highlighted,
        Color pressed)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = AddImage(rect, normal);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = CreateButtonColors(normal, highlighted, pressed);

        TMP_Text text = CreateText("Text", rect, label, fontSize, TextColor, TextAlignmentOptions.Center);
        Stretch(text.transform as RectTransform);
        return button;
    }

    private static ColorBlock CreateButtonColors(Color normal, Color highlighted, Color pressed)
    {
        Color disabled = normal;
        disabled.a = 0.48f;

        return new ColorBlock
        {
            normalColor = normal,
            highlightedColor = highlighted,
            pressedColor = pressed,
            selectedColor = normal,
            disabledColor = disabled,
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private static string GetCardName(CardData card)
    {
        if (card == null)
            return string.Empty;

        return string.IsNullOrWhiteSpace(card.CardName) ? card.name : card.CardName;
    }

    private static void ClearSelectedButton()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }
}
