using System.Linq;
using UnityEngine;

[RequireComponent(typeof(InventoryStorage))]
public class InventoryWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform cardsContent;
    [SerializeField] private CardView cardViewPrefab;

    private InventoryStorage inventoryStorage;

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        inventoryStorage = GetComponent<InventoryStorage>();

        if (inventoryStorage == null)
            DevLog.Log("InventoryStorage not found on InventoryWindow.");

        if (cardsContent == null)
            DevLog.Log("Cards Content is not assigned.");

        if (cardViewPrefab == null)
            DevLog.Log("CardSlot Prefab is not assigned.");
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
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

    public void Refresh()
    {
        if (inventoryStorage == null || cardsContent == null || cardViewPrefab == null)
            return;

        for (int i = cardsContent.childCount - 1; i >= 0; i--)
        {
            Destroy(cardsContent.GetChild(i).gameObject);
        }

        var grouped = inventoryStorage.Cards
            .GroupBy(c => c)
            .ToList();

        for (int i = 0; i < grouped.Count; i++)
        {
            var group = grouped[i];

            CardData card = group.Key;
            int copies = group.Count();

            CardView CardSlot = Instantiate(cardViewPrefab, cardsContent);
            CardSlot.Setup(card, copies, i, OnCardClicked);
        }

        DevLog.Log($"Inventory refreshed. Cards count: {inventoryStorage.Cards.Count}");
    }

    private void OnCardClicked(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= inventoryStorage.Cards.Count)
            return;

        CardData selectedCard = inventoryStorage.Cards[cardIndex];
        DevLog.Log($"Clicked card: {selectedCard.CardName}");
    }
}