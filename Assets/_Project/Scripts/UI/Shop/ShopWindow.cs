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

    private static readonly Color RowColor = new(0.58f, 0.43f, 0.36f, 1f);
    private static readonly Color RowHighlightColor = new(0.68f, 0.52f, 0.43f, 1f);
    private static readonly Color RowPressedColor = new(0.5f, 0.36f, 0.3f, 1f);
    private static readonly Color SelectedColor = new(0.68f, 0.43f, 0.19f, 1f);
    private static readonly Color TextColor = new(0.97f, 0.94f, 0.88f, 1f);
    private static readonly Color MutedTextColor = new(0.78f, 0.7f, 0.64f, 1f);
    private static readonly Color GoldColor = new(1f, 0.74f, 0.27f, 1f);
    private static readonly Color DisabledColor = new(0.4f, 0.34f, 0.31f, 0.9f);

    private ShopData activeShop;
    private NpcCharacter activeNpc;
    private PlayerCharacter activePlayer;
    private InventoryData activeInventory;
    private GameState previousState = GameState.Gameplay;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text detailsNameText;
    [SerializeField] private TMP_Text detailsDescriptionText;
    [SerializeField] private TMP_Text detailsMetaText;
    [SerializeField] private RectTransform detailsPriceRow;
    [SerializeField] private TMP_Text detailsPriceLabelText;
    [SerializeField] private TMP_Text detailsPriceValueText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text buyButtonText;
    [SerializeField] private Image detailsIcon;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private RectTransform inventoryRowTemplate;
    [SerializeField] private RectTransform shopRowTemplate;
    [SerializeField] private GameObject inventoryEmptyState;
    [SerializeField] private GameObject shopEmptyState;
    private Transform inventoryContent;
    private Transform shopContent;
    private readonly List<ShopRowView> inventoryRows = new();
    private readonly List<ShopRowView> shopRows = new();

    private int selectedShopIndex = -1;
    private CardData selectedInventoryCard;
    private bool selectedFromShop = true;
    private bool suppressGoldChangedDetailsRefresh;

    private sealed class ShopRowView
    {
        private readonly GameObject gameObject;
        private readonly Image background;
        private readonly Button button;
        private readonly Image icon;
        private readonly TMP_Text titleText;
        private readonly TMP_Text subtitleText;
        private readonly TMP_Text rightText;
        private readonly Image rightIcon;
        private bool available;

        public ShopRowView(
            GameObject gameObject,
            CardData card,
            Image background,
            Button button,
            Image icon,
            TMP_Text titleText,
            TMP_Text subtitleText,
            TMP_Text rightText,
            Image rightIcon,
            bool available)
        {
            this.gameObject = gameObject;
            Card = card;
            this.background = background;
            this.button = button;
            this.icon = icon;
            this.titleText = titleText;
            this.subtitleText = subtitleText;
            this.rightText = rightText;
            this.rightIcon = rightIcon;
            this.available = available;
            ApplyTextColors();
        }

        public CardData Card { get; }
        public RectTransform RectTransform => gameObject != null ? gameObject.transform as RectTransform : null;

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
            ApplyTextColors();
        }

        private void ApplyTextColors()
        {
            if (titleText != null)
                titleText.color = available ? TextColor : MutedTextColor;

            if (subtitleText != null)
                subtitleText.color = available ? MutedTextColor : new Color(0.58f, 0.51f, 0.47f, 1f);

            if (rightText != null)
                rightText.color = available ? GoldColor : MutedTextColor;

            if (rightIcon != null)
                rightIcon.color = available ? Color.white : MutedTextColor;

            if (icon != null && icon.sprite != null)
                icon.color = available ? Color.white : MutedTextColor;
        }

        public void SetSelected(bool selected)
        {
            Color normal = selected ? SelectedColor : available ? RowColor : DisabledColor;
            if (background != null)
                background.color = normal;

            if (button != null)
                button.colors = CreateButtonColors(normal, RowHighlightColor, RowPressedColor);
        }

        public void SetSiblingIndex(int index)
        {
            if (gameObject != null)
                gameObject.transform.SetSiblingIndex(index);
        }

        public void Destroy()
        {
            if (gameObject != null)
            {
                gameObject.SetActive(false);
                UnityEngine.Object.Destroy(gameObject);
            }
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

        Debug.LogError("ShopWindow is missing from the scene hierarchy. Add Assets/_Project/Prefabs/UI/ShopWindow.prefab to the UI canvas.");
        return null;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        PreparePrefabState();
        EnsureEventSystem();
    }

    private void PreparePrefabState()
    {
        inventoryContent = ResolveContentFromEmptyState(inventoryEmptyState);
        shopContent = ResolveContentFromEmptyState(shopEmptyState);
        closeButton ??= FindDescendant<Button>(transform, "CloseButton");

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(BuySelected);
            buyButton.onClick.AddListener(BuySelected);
        }

        SetEmptyStateVisible(inventoryEmptyState, false);
        SetEmptyStateVisible(shopEmptyState, false);
        SetTemplateVisible(inventoryRowTemplate, false);
        SetTemplateVisible(shopRowTemplate, false);

        if (inventoryContent == null || shopContent == null || inventoryRowTemplate == null || shopRowTemplate == null)
            Debug.LogError("ShopWindow prefab is missing required list content or row template references.", this);
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
        selectedShopIndex = -1;
        SetStatusText(string.Empty);

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
            goldText.text = PlayerWallet.Gold.ToString();
    }

    private void HandleGoldChanged(int _)
    {
        RefreshGold();

        if (!suppressGoldChangedDetailsRefresh)
            RefreshDetails();
    }

    private void RefreshInventory()
    {
        ClearChildren(inventoryContent, inventoryRowTemplate);
        SetEmptyStateVisible(inventoryEmptyState, false);
        inventoryRows.Clear();

        if (activeInventory == null || activeInventory.Cards == null || activeInventory.Cards.Count == 0)
        {
            SetEmptyStateVisible(inventoryEmptyState, true);
            return;
        }

        Dictionary<CardData, int> counts = activeInventory.Cards
            .Where(card => card != null)
            .GroupBy(card => card)
            .ToDictionary(group => group.Key, group => group.Count());

        List<CardData> cards = counts.Keys
            .OrderBy(GetCardName)
            .ToList();

        if (cards.Count == 0)
        {
            SetEmptyStateVisible(inventoryEmptyState, true);
            return;
        }

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
                false,
                () => SelectInventoryCard(card));
            if (row != null)
                inventoryRows.Add(row);
        }
    }

    private void RefreshShop()
    {
        ClearChildren(shopContent, shopRowTemplate);
        SetEmptyStateVisible(shopEmptyState, false);
        shopRows.Clear();

        if (activeShop == null || activeShop.Items == null || activeShop.Items.Count == 0)
        {
            SetEmptyStateVisible(shopEmptyState, true);
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
                buyPrice.ToString(),
                selected,
                available,
                true,
                () => SelectShopItem(index));
            if (row != null)
                shopRows.Add(row);
        }

        if (!hasAny)
            SetEmptyStateVisible(shopEmptyState, true);
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

        ShowPriceDetails("Price:", buyPrice, $"Stock: {stockText}", priceFirst: true);
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
        ShowPriceDetails("Sell price:", sellPrice, $"Owned: x{ownedCount}", priceFirst: true);
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
        SetStatusText(string.Empty);
        SetPriceRowVisible(false);
        detailsIcon.sprite = null;
        detailsIcon.enabled = false;
        buyButton.gameObject.SetActive(true);
        buyButton.interactable = false;
        buyButtonText.text = "Select";
    }

    private void ShowPriceDetails(string label, int amount, string plainLine, bool priceFirst)
    {
        detailsMetaText.text = plainLine;
        detailsPriceLabelText.text = label;
        detailsPriceValueText.text = amount.ToString();
        SetPriceRowVisible(true);

        if (priceFirst)
        {
            detailsPriceRow.SetSiblingIndex(0);
            detailsMetaText.transform.SetSiblingIndex(1);
        }
        else
        {
            detailsMetaText.transform.SetSiblingIndex(0);
            detailsPriceRow.SetSiblingIndex(1);
        }
    }

    private void SetPriceRowVisible(bool visible)
    {
        if (detailsPriceRow != null)
            detailsPriceRow.gameObject.SetActive(visible);
    }

    private void SetStatusText(string text)
    {
        if (statusText == null)
            return;

        bool hasText = !string.IsNullOrEmpty(text);
        statusText.text = hasText ? text : string.Empty;
        statusText.gameObject.SetActive(hasText);
    }

    private void SelectShopItem(int itemIndex)
    {
        selectedShopIndex = itemIndex;
        selectedInventoryCard = null;
        selectedFromShop = true;
        SetStatusText(string.Empty);
        ClearSelectedButton();
        RefreshRowSelectionStates();
        RefreshDetails();
    }

    private void SelectInventoryCard(CardData card)
    {
        selectedInventoryCard = card;
        selectedFromShop = false;
        SetStatusText(string.Empty);
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
            SetStatusText("Sold out.");
            RefreshShopRowsData();
            RefreshDetails();
            return;
        }

        int buyPrice = ShopManager.GetBuyPrice(activeShop, selectedShopIndex);
        if (PlayerWallet.Gold < buyPrice)
        {
            SetStatusText("Not enough.");
            RefreshDetails();
            return;
        }

        suppressGoldChangedDetailsRefresh = true;
        bool bought;
        try
        {
            bought = ShopManager.Buy(activeShop, selectedShopIndex, activeInventory);
        }
        finally
        {
            suppressGoldChangedDetailsRefresh = false;
        }

        SetStatusText(bought ? "Purchased." : "Can't buy this item.");

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
        if (inventoryRows.Count != CountInventoryGroups())
            RefreshInventory();
        else
            RefreshInventoryRowsData();

        if (shopRows.Count != CountValidShopItems())
            RefreshShop();
        else
            RefreshShopRowsData();

        RefreshRowSelectionStates();
        RefreshDetails();
    }

    private int CountInventoryGroups()
    {
        if (activeInventory == null || activeInventory.Cards == null)
            return 0;

        return activeInventory.Cards
            .Where(card => card != null)
            .Distinct()
            .Count();
    }

    private int CountValidShopItems()
    {
        if (activeShop == null || activeShop.Items == null)
            return 0;

        return activeShop.Items.Count(item => item != null && item.IsValid);
    }

    private void RefreshInventoryRowsData()
    {
        if (activeInventory == null || activeInventory.Cards == null)
        {
            ClearInventoryRows();
            SetEmptyStateVisible(inventoryEmptyState, true);
            return;
        }

        Dictionary<CardData, int> counts = activeInventory.Cards
            .Where(card => card != null)
            .GroupBy(card => card)
            .ToDictionary(group => group.Key, group => group.Count());

        List<CardData> cards = counts.Keys
            .OrderBy(GetCardName)
            .ToList();

        if (cards.Count == 0)
        {
            ClearInventoryRows();
            SetEmptyStateVisible(inventoryEmptyState, true);
            return;
        }

        SetEmptyStateVisible(inventoryEmptyState, false);

        Dictionary<CardData, ShopRowView> existingRows = inventoryRows
            .Where(row => row.Card != null)
            .ToDictionary(row => row.Card, row => row);

        List<ShopRowView> nextRows = new();
        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            bool selected = !selectedFromShop && selectedInventoryCard == card;

            if (!existingRows.TryGetValue(card, out ShopRowView row))
            {
                row = CreateItemRow(
                    inventoryContent,
                    card,
                    GetCardName(card),
                    card.ShortDescription,
                    $"x{counts[card]}",
                    selected,
                    true,
                    false,
                    () => SelectInventoryCard(card));
                if (row == null)
                    continue;
            }
            else
            {
                row.SetRightText($"x{counts[card]}");
                row.SetSelected(selected);
                existingRows.Remove(card);
            }

            row.SetSiblingIndex(i);
            nextRows.Add(row);
        }

        foreach (ShopRowView removedRow in existingRows.Values)
            removedRow.Destroy();

        inventoryRows.Clear();
        inventoryRows.AddRange(nextRows);
    }

    private void ClearInventoryRows()
    {
        for (int i = 0; i < inventoryRows.Count; i++)
            inventoryRows[i].Destroy();

        inventoryRows.Clear();
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
            shopRows[rowIndex].SetRightText(buyPrice.ToString());
            shopRows[rowIndex].SetAvailable(available);
            rowIndex++;
        }
    }

    private void SellSelected()
    {
        if (selectedInventoryCard == null || activeInventory == null)
            return;

        int sellPrice = ShopManager.GetSellPrice(activeShop, selectedInventoryCard);
        suppressGoldChangedDetailsRefresh = true;
        bool sold;
        try
        {
            sold = ShopManager.Sell(activeShop, selectedInventoryCard, activeInventory);
        }
        finally
        {
            suppressGoldChangedDetailsRefresh = false;
        }

        SetStatusText(sold ? $"Sold for {sellPrice}." : "Can't sell this item.");

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

    private ShopRowView CreateItemRow(
        Transform parent,
        CardData card,
        string title,
        string subtitle,
        string rightText,
        bool selected,
        bool available,
        bool rightShowsGoldIcon,
        Action onClick)
    {
        RectTransform template = rightShowsGoldIcon ? shopRowTemplate : inventoryRowTemplate;
        if (template == null || parent == null)
            return null;

        RectTransform row = Instantiate(template, parent);
        row.name = "ItemRow";
        row.gameObject.SetActive(true);

        Image background = row.GetComponent<Image>();
        Button button = row.GetComponent<Button>();
        Image icon = FindDescendant<Image>(row, "Icon");
        TMP_Text titleText = FindDescendant<TMP_Text>(row, "Title");
        TMP_Text subtitleText = FindDescendant<TMP_Text>(row, "Subtitle");
        TMP_Text right = FindDescendant<TMP_Text>(row, "RightText");
        Image rightIcon = FindDescendant<Image>(row, "GoldIcon");

        if (button != null)
        {
            button.targetGraphic = background;
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(() => onClick.Invoke());
        }

        if (titleText != null)
            titleText.text = title;

        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        }

        if (right != null)
            right.text = rightText;

        if (icon != null)
        {
            icon.sprite = card != null ? card.Icon : null;
            icon.enabled = card != null && card.Icon != null;
            icon.preserveAspect = true;
        }

        if (rightIcon != null)
            rightIcon.gameObject.SetActive(rightShowsGoldIcon);

        ShopRowView rowView = new(row.gameObject, card, background, button, icon, titleText, subtitleText, right, rightIcon, available);
        rowView.SetSelected(selected);
        return rowView;
    }

    private static void SetEmptyStateVisible(GameObject emptyState, bool visible)
    {
        if (emptyState != null)
            emptyState.SetActive(visible);
    }

    private static void SetTemplateVisible(RectTransform template, bool visible)
    {
        if (template != null)
            template.gameObject.SetActive(visible);
    }

    private static Transform ResolveContentFromEmptyState(GameObject emptyState)
    {
        Transform viewport = emptyState != null ? emptyState.transform.parent : null;
        return viewport != null ? viewport.Find("Content") : null;
    }

    private static T FindDescendant<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName && child.TryGetComponent(out T component))
                return component;

            T nested = FindDescendant<T>(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
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

    private static void ClearChildren(Transform parent, Transform preservedChild = null)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child == preservedChild)
                continue;

            Destroy(child.gameObject);
        }
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
