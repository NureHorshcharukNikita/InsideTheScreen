using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "NewShop", menuName = "Game/Trade/Shop")]
public sealed class ShopData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string shopKey;
    [SerializeField] private string displayName;

    [Header("Stock")]
    [SerializeField] private List<ShopItemData> items = new();

    [Header("Economy")]
    [SerializeField] private float buyMultiplier = 1.2f;
    [SerializeField] private float sellMultiplier = 0.6f;

    public string ShopKey => string.IsNullOrWhiteSpace(shopKey) ? name : shopKey;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public IReadOnlyList<ShopItemData> Items => items;
    public float BuyMultiplier => Mathf.Max(0f, buyMultiplier);
    public float SellMultiplier => Mathf.Max(0f, sellMultiplier);

    public bool HasItems
    {
        get
        {
            if (items == null)
                return false;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].IsValid)
                    return true;
            }

            return false;
        }
    }

    public int GetBuyPrice(ShopItemData item)
    {
        if (item == null || !item.IsValid)
            return 0;

        if (item.HasBuyPriceOverride)
            return item.BuyPriceOverride;

        return RoundGold(GetBaseGoldValue(item.card) * BuyMultiplier);
    }

    public int GetSellPrice(ShopItemData item)
    {
        if (item == null || !item.IsValid)
            return 0;

        if (item.HasSellPriceOverride)
            return item.SellPriceOverride;

        return GetSellPrice(item.card);
    }

    public int GetSellPrice(CardData card)
    {
        if (card == null)
            return 0;

        return RoundGold(GetBaseGoldValue(card) * SellMultiplier);
    }

    private int GetBaseGoldValue(CardData card)
    {
        return card != null ? card.BaseGoldValue : 0;
    }

    private static int RoundGold(float value)
    {
        if (value <= 0f)
            return 0;

        return Mathf.Max(1, Mathf.FloorToInt(value + 0.5f));
    }
}

[Serializable]
public sealed class ShopItemData
{
    public CardData card;

    [FormerlySerializedAs("autoBuyPrice")]
    public ShopPriceMode buyPriceMode = ShopPriceMode.AutomaticPrice;

    [Min(0)]
    [FormerlySerializedAs("price")]
    public int buyPrice = 1;

    [FormerlySerializedAs("unlimitedStock")]
    public ShopStockMode stockMode = ShopStockMode.UnlimitedStock;

    [Min(0)]
    [FormerlySerializedAs("stock")]
    public int stock = 1;

    [FormerlySerializedAs("autoSellPrice")]
    public ShopPriceMode sellPriceMode = ShopPriceMode.AutomaticPrice;

    [Min(0)]
    public int sellPrice = 1;

    public bool HasBuyPriceOverride => buyPriceMode == ShopPriceMode.ManualPrice;
    public int BuyPriceOverride => Mathf.Max(0, buyPrice);
    public bool HasSellPriceOverride => sellPriceMode == ShopPriceMode.ManualPrice;
    public int SellPriceOverride => Mathf.Max(0, sellPrice);
    public int Stock => Mathf.Max(0, stock);
    public bool HasLimitedStock => stockMode == ShopStockMode.LimitedStock;
    public bool IsValid => card != null;
}

public enum ShopPriceMode
{
    AutomaticPrice,
    ManualPrice
}

public enum ShopStockMode
{
    UnlimitedStock,
    LimitedStock
}
