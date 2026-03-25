using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InventoryStorage))]
public class InventoryWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform inventoryCardsContent;
    [SerializeField] private Transform deckCardsContent;
    [SerializeField] private CardView cardViewPrefab;

    [SerializeField] private TMP_Text inventoryCountText;
    [SerializeField] private TMP_Text deckCountText;

    [Header("Preview")]
    [SerializeField] private GameObject selectedCardContent;
    [SerializeField] private TMP_Text emptyPreviewText;
    [SerializeField] private CardView previewCard;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Buttons")]
    [SerializeField] private Button addButton;
    [SerializeField] private Button removeButton;

    private InventoryStorage inventoryStorage;

    private CardData selectedCard;
    private bool selectedFromDeck;

    public bool IsOpen => gameObject.activeSelf;

    private readonly List<CardView> inventoryPool = new();
    private readonly List<CardView> deckPool = new();

    private void Awake()
    {
        inventoryStorage = GetComponent<InventoryStorage>();

        if (inventoryStorage == null)
            DevLog.Log("InventoryStorage not found on InventoryWindow.");

        if (inventoryCardsContent == null)
            DevLog.Log("Inventory Cards Content is not assigned.");

        if (deckCardsContent == null)
            DevLog.Log("Deck Cards Content is not assigned.");

        if (cardViewPrefab == null)
            DevLog.Log("CardView Prefab is not assigned.");

        if (selectedCardContent == null)
            DevLog.Log("Selected Card Content is not assigned.");

        if (emptyPreviewText == null)
            DevLog.Log("Empty Preview Text is not assigned.");

        if (previewCard == null)
            DevLog.Log("Preview Card is not assigned.");

        if (descriptionText == null)
            DevLog.Log("Description Text is not assigned.");

        InitialCleanupCardViews();
    }

    private void InitialCleanupCardViews()
    {
        RemoveCardViews(inventoryCardsContent);
        RemoveCardViews(deckCardsContent);
    }

    private void RemoveCardViews(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child.TryGetComponent<CardView>(out _))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void OnEnable()
    {
        Refresh();
        ClearPreview();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
        ClearPreview();
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
        ClearCards(inventoryPool);
        ClearCards(deckPool);

        if (inventoryStorage == null ||
            inventoryCardsContent == null ||
            deckCardsContent == null ||
            cardViewPrefab == null)
        {
            SetEmptyCounters();
            return;
        }

        DrawInventory();

        if (HasDeck())
            DrawDeck();

        UpdateCounters();
    }

    private void ClearCards(List<CardView> pool)
    {
        for (int i = 0; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }

    private void DrawInventory()
    {
        if (inventoryStorage == null || inventoryStorage.Cards == null)
            return;

        DrawCards(
            inventoryStorage.Cards,
            inventoryCardsContent,
            OnInventoryCardClicked,
            inventoryPool);
    }

    private void DrawDeck()
    {
        if (!HasDeck())
            return;

        DrawCards(
            DeckProvider.Instance.Deck.Cards,
            deckCardsContent,
            OnDeckCardClicked,
            deckPool);
    }

    private void DrawCards(
        IReadOnlyList<CardData> cards,
        Transform content,
        System.Action<int> onClick,
        List<CardView> pool)
    {
        if (cards == null || content == null || cardViewPrefab == null)
            return;

        Dictionary<CardData, int> counts = cards
            .GroupBy(c => c)
            .ToDictionary(g => g.Key, g => g.Count());

        List<CardData> sortedCards = GetSortedUniqueCards(cards);

        for (int i = 0; i < sortedCards.Count; i++)
        {
            var card = sortedCards[i];

            CardView cardView = GetFromPool(pool, content);
            cardView.Setup(card, counts[card], i, onClick);
        }

        UpdateContentHeight(content, sortedCards.Count);
    }

    private CardView GetFromPool(List<CardView> pool, Transform parent)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeSelf)
            {
                pool[i].gameObject.SetActive(true);
                return pool[i];
            }
        }

        var newCard = Instantiate(cardViewPrefab, parent);
        pool.Add(newCard);
        return newCard;
    }

    private void UpdateContentHeight(Transform content, int itemCount)
    {
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        RectTransform rt = content.GetComponent<RectTransform>();

        int columns = grid.constraintCount;
        int rows = Mathf.CeilToInt((float)itemCount / columns);

        float height =
            grid.padding.top +
            grid.padding.bottom +
            rows * grid.cellSize.y +
            (rows - 1) * grid.spacing.y;

        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    private List<CardData> GetSortedUniqueCards(IReadOnlyList<CardData> cards)
    {
        if (cards == null)
            return new List<CardData>();

        return cards
            .GroupBy(c => c)
            .Select(g => g.Key)
            .OrderBy(c => c.CardName)
            .ToList();
    }

    private void OnInventoryCardClicked(int index)
    {
        if (inventoryStorage == null || inventoryStorage.Cards == null)
            return;

        var sortedCards = GetSortedUniqueCards(inventoryStorage.Cards);

        if (index < 0 || index >= sortedCards.Count)
            return;

        SelectCard(sortedCards[index], false);
    }

    private void OnDeckCardClicked(int index)
    {
        if (!HasDeck())
            return;

        var sortedCards = GetSortedUniqueCards(DeckProvider.Instance.Deck.Cards);

        if (index < 0 || index >= sortedCards.Count)
            return;

        SelectCard(sortedCards[index], true);
    }

    private void SelectCard(CardData card, bool fromDeck)
    {
        if (card == null)
            return;

        selectedCard = card;
        selectedFromDeck = fromDeck;

        selectedCardContent.SetActive(true);
        emptyPreviewText.gameObject.SetActive(false);

        previewCard.gameObject.SetActive(true);
        previewCard.Setup(card, null, 0, null);

        descriptionText.text = card.Description;

        Canvas.ForceUpdateCanvases();

        var scrollRect = descriptionText.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        bool hasDeck = HasDeck();

        bool canAdd = hasDeck && CanAddToDeck(card);
        bool canRemove = hasDeck && DeckProvider.Instance.Deck.Cards.Contains(card);

        addButton.interactable = canAdd;
        removeButton.interactable = canRemove;
    }

    private void ClearPreview()
    {
        selectedCard = null;
        selectedFromDeck = false;

        selectedCardContent.SetActive(false);

        emptyPreviewText.gameObject.SetActive(true);
        emptyPreviewText.text = "Select a card to view details";

        previewCard.gameObject.SetActive(false);

        descriptionText.text = "";

        addButton.interactable = false;
        removeButton.interactable = false;
    }

    public void AddSelectedToDeck()
    {
        if (selectedCard == null)
            return;

        if (!CanAddToDeck(selectedCard))
            return;

        DeckProvider.Instance.Deck.Add(selectedCard);

        Refresh();
        SelectCard(selectedCard, false);
    }

    public void RemoveSelectedFromDeck()
    {
        if (selectedCard == null)
            return;

        if (!HasDeck())
            return;

        DeckProvider.Instance.Deck.Remove(selectedCard);

        Refresh();
        SelectCard(selectedCard, false);
    }

    private bool CanAddToDeck(CardData card)
    {
        int inventoryCopies = inventoryStorage.Cards.Count(c => c == card);
        int deckCopies = DeckProvider.Instance.Deck.Cards.Count(c => c == card);

        return deckCopies < inventoryCopies;
    }

    private bool HasDeck()
    {
        return DeckProvider.Instance != null && DeckProvider.Instance.Deck != null;
    }

    private void UpdateCounters()
    {
        int inventoryCount = inventoryStorage.Cards.Count;
        inventoryCountText.text = $"{inventoryCount}/60";

        if (HasDeck())
            deckCountText.text = $"{DeckProvider.Instance.Deck.Count}/{DeckProvider.Instance.Deck.MaxCount}";
        else
            deckCountText.text = "0/0";
    }

    private void SetEmptyCounters()
    {
        inventoryCountText.text = "0/0";
        deckCountText.text = "0/0";
    }
}