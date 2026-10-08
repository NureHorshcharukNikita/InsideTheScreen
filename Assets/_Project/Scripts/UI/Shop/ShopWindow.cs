using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ShopWindow : MonoBehaviour
{
    private static ShopWindow instance;

    // Rows darken on hover like the other sprite buttons; sold out rows stay dimmed.
    private static readonly Color RowHighlightColor = new(0.82f, 0.82f, 0.82f, 1f);
    private static readonly Color RowPressedColor = new(0.68f, 0.68f, 0.68f, 1f);
    private static readonly Color UnavailableRowColor = new(0.62f, 0.62f, 0.62f, 1f);
    private static readonly Color TextColor = new(0.97f, 0.94f, 0.88f, 1f);
    private static readonly Color MutedTextColor = new(0.78f, 0.7f, 0.64f, 1f);
    private static readonly Color GoldColor = new(1f, 0.74f, 0.27f, 1f);
    private const string NoMatchesMessage = "No cards found";
    private const float GoldDeltaWidth = 230f;
    private const float GoldDeltaIconSize = 40f;

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
    [SerializeField] private GameObject statusPanel;
    [SerializeField] private CanvasGroup statusCanvasGroup;
    [SerializeField] private TMP_Text buyButtonText;
    [SerializeField] private Image detailsIcon;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private RectTransform inventoryRowTemplate;
    [SerializeField] private RectTransform shopRowTemplate;
    [SerializeField] private GameObject inventoryEmptyState;
    [SerializeField] private GameObject shopEmptyState;

    // The same quantity dialog as the inventory "Add to deck" prompt.
    [SerializeField] private QuantitySelectionPrompt quantityPrompt;

    [Header("Lists")]
    [SerializeField] private Sprite rowSprite;
    [SerializeField] private Sprite selectedRowSprite;
    [SerializeField] private CardSearchField inventorySearch;
    [SerializeField] private CardSearchField shopSearch;
    private Transform inventoryContent;
    private Transform shopContent;
    private readonly List<ShopRowView> inventoryRows = new();
    private readonly List<ShopRowView> shopRows = new();
    private Coroutine statusToastCoroutine;
    private RectTransform goldDeltaRect;
    private TMP_Text goldDeltaText;
    private CanvasGroup goldDeltaCanvasGroup;
    private Coroutine goldDeltaCoroutine;
    private int trackedGold;
    private bool isGoldTracked;
    private bool isPrepared;
    private string inventoryEmptyMessage;
    private string shopEmptyMessage;

    private int selectedShopIndex = -1;
    private CardData selectedInventoryCard;
    private bool selectedFromShop = true;
    private bool suppressGoldChangedDetailsRefresh;

    private sealed class ShopRowView
    {
        private readonly GameObject gameObject;
        private readonly Image background;
        private readonly Sprite normalSprite;
        private readonly Sprite selectedSprite;
        private readonly Image icon;
        private readonly TMP_Text titleText;
        private readonly TMP_Text subtitleText;
        private readonly TMP_Text rightText;
        private readonly Image rightIcon;
        private bool available;
        private bool selected;

        public ShopRowView(
            GameObject gameObject,
            CardData card,
            int shopIndex,
            Image background,
            Sprite normalSprite,
            Sprite selectedSprite,
            Image icon,
            TMP_Text titleText,
            TMP_Text subtitleText,
            TMP_Text rightText,
            Image rightIcon,
            bool available)
        {
            this.gameObject = gameObject;
            Card = card;
            ShopIndex = shopIndex;
            this.background = background;
            this.normalSprite = normalSprite != null ? normalSprite : background != null ? background.sprite : null;
            this.selectedSprite = selectedSprite;
            this.icon = icon;
            this.titleText = titleText;
            this.subtitleText = subtitleText;
            this.rightText = rightText;
            this.rightIcon = rightIcon;
            this.available = available;
            ApplyTextColors();
        }

        public CardData Card { get; }
        // Index in the shop item list for "For Sale" rows; -1 for inventory rows.
        public int ShopIndex { get; }
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
            ApplyBackground();
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

        public void SetSelected(bool value)
        {
            selected = value;
            ApplyBackground();
        }

        // The selected row uses the lighted panel; the button tint only darkens it on hover.
        private void ApplyBackground()
        {
            if (background == null)
                return;

            background.sprite = selected && selectedSprite != null ? selectedSprite : normalSprite;
            background.color = available || selected ? Color.white : UnavailableRowColor;
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

        if (!window.isPrepared)
            window.PreparePrefabState();

        if (!window.isPrepared)
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

        // The window may be left open in the editor for layout work. It starts closed in play
        // mode; when Show activates it for the first time, the shop is already assigned.
        if (activeShop == null)
            gameObject.SetActive(false);
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

        if (quantityPrompt == null)
            quantityPrompt = QuantitySelectionPrompt.Create(GetOverlayParent(), detailsNameText);

        PrepareStatusToast();
        PrepareGoldDelta();
        PrepareSearch();

        inventoryEmptyMessage ??= GetEmptyStateText(inventoryEmptyState);
        shopEmptyMessage ??= GetEmptyStateText(shopEmptyState);

        SetEmptyStateVisible(inventoryEmptyState, false);
        SetEmptyStateVisible(shopEmptyState, false);
        SetTemplateVisible(inventoryRowTemplate, false);
        SetTemplateVisible(shopRowTemplate, false);

        isPrepared = inventoryContent != null
            && shopContent != null
            && inventoryRowTemplate != null
            && shopRowTemplate != null
            && titleText != null
            && goldText != null
            && detailsNameText != null
            && detailsDescriptionText != null
            && detailsMetaText != null
            && detailsPriceRow != null
            && detailsPriceLabelText != null
            && detailsPriceValueText != null
            && detailsIcon != null
            && buyButton != null
            && buyButtonText != null
            && quantityPrompt != null;

        if (!isPrepared)
            Debug.LogError("ShopWindow prefab is missing required UI references.", this);
    }

    private void PrepareSearch()
    {
        if (inventorySearch != null)
        {
            inventorySearch.QueryChanged -= HandleInventorySearchChanged;
            inventorySearch.QueryChanged += HandleInventorySearchChanged;
        }

        if (shopSearch != null)
        {
            shopSearch.QueryChanged -= HandleShopSearchChanged;
            shopSearch.QueryChanged += HandleShopSearchChanged;
        }
    }

    private void HandleInventorySearchChanged(string _)
    {
        RefreshInventory();
    }

    private void HandleShopSearchChanged(string _)
    {
        RefreshShop();
    }

    private void PrepareStatusToast()
    {
        if (statusPanel == null || statusText == null)
            return;

        RectTransform panelRect = statusPanel.transform as RectTransform;
        RectTransform textRect = statusText.rectTransform;
        if (panelRect == null || textRect == null)
            return;

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(600f, 120f);
        panelRect.localRotation = Quaternion.identity;
        panelRect.localScale = Vector3.one;

        textRect.SetParent(panelRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(-32f, -20f);
        textRect.localRotation = Quaternion.identity;
        textRect.localScale = Vector3.one;
        textRect.SetAsLastSibling();

        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = TextColor;
        statusText.enableAutoSizing = true;
        statusText.fontSizeMin = 18f;
        statusText.fontSizeMax = 28f;
        statusText.raycastTarget = false;

        statusPanel.SetActive(false);
    }

    // The +N / -N popup is the amount with a gold icon next to it. Its pivot sits on the icon
    // center, so the popup icon moves straight under the gold icon in the top bar.
    private void PrepareGoldDelta()
    {
        if (goldText == null || goldDeltaRect != null)
            return;

        GameObject deltaObject = new("GoldDelta", typeof(RectTransform), typeof(CanvasGroup));
        goldDeltaRect = deltaObject.GetComponent<RectTransform>();
        RectTransform overlayRoot = statusPanel != null
            ? statusPanel.transform.parent as RectTransform
            : transform as RectTransform;
        goldDeltaRect.SetParent(overlayRoot != null ? overlayRoot : transform, false);
        goldDeltaRect.anchorMin = new Vector2(0.5f, 0.5f);
        goldDeltaRect.anchorMax = new Vector2(0.5f, 0.5f);
        goldDeltaRect.pivot = new Vector2((GoldDeltaWidth - GoldDeltaIconSize * 0.5f) / GoldDeltaWidth, 0.5f);
        goldDeltaRect.anchoredPosition = Vector2.zero;
        goldDeltaRect.sizeDelta = new Vector2(GoldDeltaWidth, 44f);

        GameObject textObject = new("Amount", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(goldDeltaRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = new Vector2(-(GoldDeltaIconSize + 6f), 0f);

        GameObject iconObject = new("GoldIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(goldDeltaRect, false);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(1f, 0.5f);
        iconRect.pivot = new Vector2(1f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(GoldDeltaIconSize, GoldDeltaIconSize);
        Image icon = iconObject.GetComponent<Image>();
        Image goldIcon = GetGoldIcon();
        icon.sprite = goldIcon != null ? goldIcon.sprite : null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = icon.sprite != null;

        goldDeltaText = textObject.GetComponent<TMP_Text>();
        goldDeltaText.font = goldText.font;
        goldDeltaText.fontSharedMaterial = goldText.fontSharedMaterial;
        goldDeltaText.fontSize = 30f;
        goldDeltaText.fontStyle = FontStyles.Bold;
        goldDeltaText.alignment = TextAlignmentOptions.MidlineRight;
        goldDeltaText.textWrappingMode = TextWrappingModes.NoWrap;
        goldDeltaText.raycastTarget = false;

        goldDeltaCanvasGroup = deltaObject.GetComponent<CanvasGroup>();
        goldDeltaCanvasGroup.interactable = false;
        goldDeltaCanvasGroup.blocksRaycasts = false;
        deltaObject.SetActive(false);
    }

    private Image GetGoldIcon()
    {
        return goldText != null ? FindDescendant<Image>(goldText.transform.parent, "GoldIcon") : null;
    }

    private Transform GetOverlayParent()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        return canvas != null && canvas.rootCanvas != null
            ? canvas.rootCanvas.transform
            : transform;
    }

    private void OnEnable()
    {
        trackedGold = PlayerWallet.Gold;
        isGoldTracked = true;
        PlayerWallet.GoldChanged += HandleGoldChanged;
        RefreshGold();
    }

    private void OnDisable()
    {
        PlayerWallet.GoldChanged -= HandleGoldChanged;
        isGoldTracked = false;
        quantityPrompt?.Hide();
        HideStatusToast();
        HideGoldDelta();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (GameInputSettings.GetKeyDown(GameInputAction.Close))
        {
            if (quantityPrompt != null && quantityPrompt.IsOpen)
            {
                quantityPrompt.Hide();
                return;
            }

            Close();
        }
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
        quantityPrompt?.Hide();

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
        if (inventorySearch != null)
            inventorySearch.Clear();
        if (shopSearch != null)
            shopSearch.Clear();
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

    private void HandleGoldChanged(int newGold)
    {
        int delta = isGoldTracked ? newGold - trackedGold : 0;
        trackedGold = newGold;
        isGoldTracked = true;

        RefreshGold();

        if (delta != 0)
            ShowGoldDelta(delta);

        if (!suppressGoldChangedDetailsRefresh)
            RefreshDetails();
    }

    private void ShowGoldDelta(int delta)
    {
        if (goldDeltaRect == null || goldDeltaText == null || goldDeltaCanvasGroup == null)
            return;

        if (goldDeltaCoroutine != null)
            StopCoroutine(goldDeltaCoroutine);

        goldDeltaText.text = delta > 0 ? $"+{delta}" : delta.ToString();
        goldDeltaText.color = delta > 0
            ? new Color(0.56f, 0.89f, 0.53f, 1f)
            : new Color(1f, 0.49f, 0.4f, 1f);
        goldDeltaRect.gameObject.SetActive(true);
        goldDeltaRect.SetAsLastSibling();
        goldDeltaCoroutine = StartCoroutine(AnimateGoldDelta(delta > 0));
    }

    private IEnumerator AnimateGoldDelta(bool isGain)
    {
        RectTransform rect = goldDeltaRect;
        Vector2 goldPosition = GetGoldDeltaStartPosition();
        Vector2 startPosition = isGain
            ? goldPosition + Vector2.down * 54f
            : goldPosition;
        Vector2 endPosition = isGain
            ? goldPosition
            : goldPosition + Vector2.down * 54f;
        const float duration = 1.05f;
        const float fadeStart = 0.45f;

        rect.anchoredPosition = startPosition;
        rect.localScale = Vector3.one * 1.12f;
        goldDeltaCanvasGroup.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            rect.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, easedProgress);
            rect.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, easedProgress);

            if (progress > fadeStart)
                goldDeltaCanvasGroup.alpha = 1f - Mathf.InverseLerp(fadeStart, 1f, progress);

            yield return null;
        }

        goldDeltaCoroutine = null;
        HideGoldDelta();
    }

    private Vector2 GetGoldDeltaStartPosition()
    {
        RectTransform parentRect = goldDeltaRect.parent as RectTransform;
        if (parentRect == null || goldText == null)
            return Vector2.zero;

        // Under the center of the top bar gold icon; without the icon, at the end of the amount.
        Canvas.ForceUpdateCanvases();
        Image goldIcon = GetGoldIcon();
        RectTransform anchor = goldIcon != null ? goldIcon.rectTransform : goldText.rectTransform;
        Rect anchorRect = anchor.rect;
        Vector3 worldPosition = anchor.TransformPoint(goldIcon != null
            ? (Vector3)anchorRect.center
            : new Vector3(anchorRect.xMax, anchorRect.center.y, 0f));
        Vector3 localPosition = parentRect.InverseTransformPoint(worldPosition);
        return new Vector2(localPosition.x, localPosition.y);
    }

    private void HideGoldDelta()
    {
        if (goldDeltaCoroutine != null)
        {
            StopCoroutine(goldDeltaCoroutine);
            goldDeltaCoroutine = null;
        }

        if (goldDeltaCanvasGroup != null)
            goldDeltaCanvasGroup.alpha = 1f;

        if (goldDeltaRect != null)
        {
            goldDeltaRect.anchoredPosition = Vector2.zero;
            goldDeltaRect.localScale = Vector3.one;
            goldDeltaRect.gameObject.SetActive(false);
        }
    }

    private void RefreshInventory()
    {
        ClearChildren(inventoryContent, inventoryRowTemplate);
        inventoryRows.Clear();

        Dictionary<CardData, int> counts = GetInventoryCounts();
        List<CardData> cards = GetVisibleInventoryCards(counts);
        UpdateListState(inventoryEmptyState, inventorySearch, inventoryEmptyMessage, counts.Count > 0, cards.Count > 0);

        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];
            int count = counts[card];
            bool selected = !selectedFromShop && selectedInventoryCard == card;
            ShopRowView row = CreateItemRow(
                inventoryContent,
                card,
                -1,
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

    private Dictionary<CardData, int> GetInventoryCounts()
    {
        if (activeInventory == null || activeInventory.Cards == null)
            return new Dictionary<CardData, int>();

        return activeInventory.Cards
            .Where(card => card != null)
            .GroupBy(card => card)
            .ToDictionary(group => group.Key, group => group.Count());
    }

    // Owned cards that pass the inventory search, in name order.
    private List<CardData> GetVisibleInventoryCards(Dictionary<CardData, int> counts)
    {
        string query = inventorySearch != null ? inventorySearch.Query : string.Empty;
        return counts.Keys
            .Where(card => CardSearchField.Matches(GetCardName(card), query))
            .OrderBy(GetCardName)
            .ToList();
    }

    // Valid shop items that pass the shop search, in shop order.
    private List<int> GetVisibleShopIndices()
    {
        List<int> indices = new();
        if (activeShop == null || activeShop.Items == null)
            return indices;

        string query = shopSearch != null ? shopSearch.Query : string.Empty;
        for (int i = 0; i < activeShop.Items.Count; i++)
        {
            ShopItemData item = activeShop.Items[i];
            if (item != null && item.IsValid && CardSearchField.Matches(GetCardName(item.card), query))
                indices.Add(i);
        }

        return indices;
    }

    private void RefreshShop()
    {
        ClearChildren(shopContent, shopRowTemplate);
        shopRows.Clear();

        List<int> indices = GetVisibleShopIndices();
        bool hasItems = activeShop != null && activeShop.Items != null
            && activeShop.Items.Any(item => item != null && item.IsValid);
        UpdateListState(shopEmptyState, shopSearch, shopEmptyMessage, hasItems, indices.Count > 0);

        foreach (int index in indices)
        {
            ShopItemData item = activeShop.Items[index];
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
                index,
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
    }

    // The search bar is hidden while its list has nothing to search. The empty state shows
    // the list icon when the list is empty and only a short note when the search finds nothing.
    private static void UpdateListState(GameObject emptyState, CardSearchField search, string emptyMessage,
        bool hasItems, bool hasVisibleItems)
    {
        if (search != null)
        {
            if (!hasItems)
                search.Clear();

            Transform searchBar = search.transform.parent;
            (searchBar != null ? searchBar : search.transform).gameObject.SetActive(hasItems);
        }

        if (emptyState == null)
            return;

        emptyState.SetActive(!hasVisibleItems);
        if (hasVisibleItems)
            return;

        if (emptyState.TryGetComponent(out TMP_Text text))
            text.text = hasItems ? NoMatchesMessage : emptyMessage;

        Transform icon = emptyState.transform.Find("Icon");
        if (icon != null)
            icon.gameObject.SetActive(!hasItems);
    }

    private static string GetEmptyStateText(GameObject emptyState)
    {
        return emptyState != null && emptyState.TryGetComponent(out TMP_Text text) ? text.text : string.Empty;
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
        if (!hasText)
        {
            HideStatusToast();
            return;
        }

        ShowStatusToast("Notice", text);
    }

    private void ShowStatusToast(string title, string message)
    {
        if (statusText == null || statusPanel == null)
            return;

        if (statusToastCoroutine != null)
            StopCoroutine(statusToastCoroutine);

        statusText.text = $"<color=#FFD166><b>{title}</b></color>\n<color=#F7EFE2>{message}</color>";
        statusPanel.SetActive(true);
        statusPanel.transform.SetAsLastSibling();
        statusToastCoroutine = StartCoroutine(AnimateStatusToast());
    }

    private IEnumerator AnimateStatusToast()
    {
        RectTransform rect = statusPanel.transform as RectTransform;
        const float showDuration = 0.16f;
        const float holdDuration = 1.9f;
        const float hideDuration = 0.24f;

        float elapsed = 0f;
        while (elapsed < showDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / showDuration);
            if (statusCanvasGroup != null)
                statusCanvasGroup.alpha = progress;
            if (rect != null)
                rect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, progress);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(holdDuration);

        elapsed = 0f;
        while (elapsed < hideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / hideDuration);
            if (statusCanvasGroup != null)
                statusCanvasGroup.alpha = 1f - progress;
            yield return null;
        }

        statusToastCoroutine = null;
        HideStatusToast();
    }

    private void HideStatusToast()
    {
        if (statusToastCoroutine != null)
        {
            StopCoroutine(statusToastCoroutine);
            statusToastCoroutine = null;
        }

        if (statusCanvasGroup != null)
            statusCanvasGroup.alpha = 1f;

        if (statusPanel != null)
        {
            statusPanel.transform.localScale = Vector3.one;
            statusPanel.SetActive(false);
        }

        if (statusText != null)
            statusText.text = string.Empty;
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
            ShowSellQuantityPrompt();
            return;
        }

        if (!TryGetSelectedShopItem(out ShopItemData item))
            return;

        if (ShopManager.GetRemainingStock(activeShop, selectedShopIndex) <= 0)
        {
            ShowStatusToast("Unable to purchase", "Sold out.");
            RefreshShopRowsData();
            RefreshDetails();
            return;
        }

        int buyPrice = ShopManager.GetBuyPrice(activeShop, selectedShopIndex);
        if (PlayerWallet.Gold < buyPrice)
        {
            ShowStatusToast("Unable to purchase", "Not enough gold.");
            RefreshDetails();
            return;
        }

        int remainingStock = ShopManager.GetRemainingStock(activeShop, selectedShopIndex);
        int affordableQuantity = buyPrice > 0 ? PlayerWallet.Gold / buyPrice : remainingStock;
        int maxQuantity = Mathf.Min(remainingStock, affordableQuantity);
        if (maxQuantity == int.MaxValue)
            maxQuantity = 999999;

        int itemIndex = selectedShopIndex;
        quantityPrompt.Show(
            "Buy",
            GetCardName(item.card),
            "Buy",
            maxQuantity,
            quantity => BuySelected(itemIndex, quantity));
    }

    private void BuySelected(int itemIndex, int quantity)
    {
        int buyPrice = ShopManager.GetBuyPrice(activeShop, itemIndex);
        suppressGoldChangedDetailsRefresh = true;
        int purchasedCount;
        try
        {
            purchasedCount = ShopManager.Buy(activeShop, itemIndex, activeInventory, quantity);
        }
        finally
        {
            suppressGoldChangedDetailsRefresh = false;
        }

        if (purchasedCount > 0)
        {
            CardData card = activeShop.Items[itemIndex].card;
            ShowStatusToast("Purchased", $"{GetCardName(card)} x{purchasedCount}.");
        }
        else
        {
            ShowStatusToast("Unable to purchase", "The transaction could not be completed.");
        }

        if (purchasedCount > 0 && ShopManager.GetRemainingStock(activeShop, itemIndex) <= 0)
            selectedShopIndex = FindFirstAvailableShopIndex();

        RefreshAfterTransaction();
    }

    private void RefreshRowSelectionStates()
    {
        foreach (ShopRowView row in inventoryRows)
            row.SetSelected(!selectedFromShop && selectedInventoryCard == row.Card);

        foreach (ShopRowView row in shopRows)
            row.SetSelected(selectedFromShop && selectedShopIndex == row.ShopIndex);
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
        return GetVisibleInventoryCards(GetInventoryCounts()).Count;
    }

    private int CountValidShopItems()
    {
        return GetVisibleShopIndices().Count;
    }

    private void RefreshInventoryRowsData()
    {
        Dictionary<CardData, int> counts = GetInventoryCounts();
        List<CardData> cards = GetVisibleInventoryCards(counts);
        UpdateListState(inventoryEmptyState, inventorySearch, inventoryEmptyMessage, counts.Count > 0, cards.Count > 0);

        if (cards.Count == 0)
        {
            ClearInventoryRows();
            return;
        }

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
                    -1,
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
        if (activeShop?.Items == null)
            return;

        foreach (ShopRowView row in shopRows)
        {
            int i = row.ShopIndex;
            if (i < 0 || i >= activeShop.Items.Count)
                continue;

            ShopItemData item = activeShop.Items[i];
            if (item == null || !item.IsValid)
                continue;

            int remainingStock = ShopManager.GetRemainingStock(activeShop, i);
            bool available = remainingStock > 0;
            int buyPrice = ShopManager.GetBuyPrice(activeShop, i);
            string stockText = item.HasLimitedStock
                ? available ? $"Stock {remainingStock}" : "Sold out"
                : "Unlimited";

            row.SetSubtitle(stockText);
            row.SetRightText(buyPrice.ToString());
            row.SetAvailable(available);
        }
    }

    private void ShowSellQuantityPrompt()
    {
        if (selectedInventoryCard == null || activeInventory == null)
            return;

        int ownedCount = activeInventory.Cards.Count(card => card == selectedInventoryCard);
        CardData card = selectedInventoryCard;
        quantityPrompt.Show(
            "Sell",
            GetCardName(card),
            "Sell",
            ownedCount,
            quantity => SellSelected(card, quantity));
    }

    private void SellSelected(CardData card, int quantity)
    {
        if (card == null || activeInventory == null)
            return;

        int sellPrice = ShopManager.GetSellPrice(activeShop, card);
        suppressGoldChangedDetailsRefresh = true;
        int soldCount;
        try
        {
            soldCount = ShopManager.Sell(activeShop, card, activeInventory, quantity);
        }
        finally
        {
            suppressGoldChangedDetailsRefresh = false;
        }

        int totalPrice = sellPrice * soldCount;
        if (soldCount > 0)
            ShowStatusToast("Sold", $"{GetCardName(card)} x{soldCount}.");
        else
            ShowStatusToast("Unable to sell", "The transaction could not be completed.");

        if (activeInventory.Cards == null || !activeInventory.Cards.Contains(card))
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
        int shopIndex,
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
            button.transition = Selectable.Transition.ColorTint;
            button.colors = CreateButtonColors(Color.white, RowHighlightColor, RowPressedColor);
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

        ShopRowView rowView = new(row.gameObject, card, shopIndex, background, rowSprite, selectedRowSprite,
            icon, titleText, subtitleText, right, rightIcon, available);
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
