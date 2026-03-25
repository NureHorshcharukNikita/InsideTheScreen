using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InventoryStorage))]
public class InventoryWindow : MonoBehaviour
{
    private enum CardListMode
    {
        Inventory,
        Deck
    }

    [Header("UI References")]
    [SerializeField] private Transform cardsContent;
    [SerializeField] private CardView cardViewPrefab;

    [SerializeField] private TMP_Text inventoryCountText;
    [SerializeField] private TMP_Text deckCountText;

    [SerializeField] private Image inventoryTabImage;
    [SerializeField] private Image deckTabImage;
    [SerializeField] private Color selectedTabColor;
    [SerializeField] private Color normalTabColor;

    private InventoryStorage inventoryStorage;
    private CardListMode currentMode = CardListMode.Inventory;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        inventoryStorage = GetComponent<InventoryStorage>();

        if (inventoryStorage == null)
            DevLog.Log("InventoryStorage not found on InventoryWindow.");

        if (cardsContent == null)
            DevLog.Log("Cards Content is not assigned.");

        if (cardViewPrefab == null)
            DevLog.Log("CardView Prefab is not assigned.");

        if (DeckProvider.Instance == null)
            DevLog.Log("DeckProvider instance not found.");
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void OnEnable()
    {
        UpdateTabVisuals();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
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

    public void ShowInventory()
    {
        currentMode = CardListMode.Inventory;
        UpdateTabVisuals();
        Refresh();
    }

    public void ShowDeck()
    {
        currentMode = CardListMode.Deck;
        UpdateTabVisuals();
        Refresh();
    }

    public void Refresh()
    {
        if (inventoryStorage == null || cardsContent == null || cardViewPrefab == null || DeckProvider.Instance == null)
            return;

        ClearCards();

        if (currentMode == CardListMode.Inventory)
            DrawInventory();
        else
            DrawDeck();

        UpdateCounters();
    }

    private void ClearCards()
    {
        for (int i = cardsContent.childCount - 1; i >= 0; i--)
            Destroy(cardsContent.GetChild(i).gameObject);
    }

    private void DrawInventory()
    {
        DrawCards(inventoryStorage.Cards, "Inventory");
    }

    private void DrawDeck()
    {
        DrawCards(DeckProvider.Instance.Deck.Cards, "Deck");
    }

    private void DrawCards(IReadOnlyList<CardData> cards, string label)
    {
        var grouped = cards
            .GroupBy(c => c)
            .ToList();

        for (int i = 0; i < grouped.Count; i++)
        {
            var group = grouped[i];
            CardData card = group.Key;
            int copies = group.Count();

            CardView cardView = Instantiate(cardViewPrefab, cardsContent);
            cardView.Setup(card, copies, i, OnCardClicked);
        }

        DevLog.Log($"{label} refreshed. Cards count: {cards.Count}");
    }

    private void OnCardClicked(int cardIndex)
    {
        if (currentMode == CardListMode.Inventory)
            AddFromInventory(cardIndex);
        else
            RemoveFromDeck(cardIndex);
    }

    private void AddFromInventory(int index)
    {
        var grouped = inventoryStorage.Cards
            .GroupBy(c => c)
            .ToList();

        if (index < 0 || index >= grouped.Count)
            return;

        CardData card = grouped[index].Key;

        DeckProvider.Instance.Deck.Add(card);
        Refresh();
    }

    private void RemoveFromDeck(int index)
    {
        var grouped = DeckProvider.Instance.Deck.Cards
            .GroupBy(c => c)
            .ToList();

        if (index < 0 || index >= grouped.Count)
            return;

        CardData card = grouped[index].Key;

        DeckProvider.Instance.Deck.Remove(card);
        Refresh();
    }

    private void UpdateTabVisuals()
    {
        if (inventoryTabImage != null)
            inventoryTabImage.color = currentMode == CardListMode.Inventory
                ? selectedTabColor
                : normalTabColor;

        if (deckTabImage != null)
            deckTabImage.color = currentMode == CardListMode.Deck
                ? selectedTabColor
                : normalTabColor;
    }

    private void UpdateCounters()
    {
        inventoryCountText.text = $"{inventoryStorage.Cards.Count}/60";
        deckCountText.text = $"{DeckProvider.Instance.Deck.Count}/{DeckProvider.Instance.Deck.MaxCount}";
    }
}