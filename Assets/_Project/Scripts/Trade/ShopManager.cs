using System;
using System.Collections.Generic;

public static class ShopManager
{
    private static readonly List<ShopProgressSaveData> Shops = new();

    public static event Action<ShopData, ShopItemData> ItemPurchased;
    public static event Action<ShopData, CardData, int> ItemSold;

    public static bool Buy(ShopData shop, int itemIndex, InventoryData inventory)
    {
        if (!TryGetItem(shop, itemIndex, out ShopItemData item) || inventory == null)
            return false;

        if (GetRemainingStock(shop, itemIndex) <= 0)
            return false;

        int price = GetBuyPrice(shop, itemIndex);
        if (!PlayerWallet.TrySpendGold(price))
            return false;

        inventory.AddCard(item.card);

        if (item.HasLimitedStock)
        {
            ShopProgressSaveData progress = GetOrCreateProgress(shop.ShopKey, itemIndex);
            progress.purchasedCount++;
        }

        ItemPurchased?.Invoke(shop, item);
        ExplorationPlayerSession.SaveShopProgress();
        return true;
    }

    public static bool Sell(ShopData shop, CardData card, InventoryData inventory)
    {
        if (shop == null || card == null || inventory == null)
            return false;

        int price = GetSellPrice(shop, card);
        if (price <= 0)
            return false;

        inventory.RemoveCard(card);
        PlayerWallet.AddGold(price);
        ItemSold?.Invoke(shop, card, price);
        return true;
    }

    public static int GetSellPrice(ShopData shop, CardData card)
    {
        if (shop == null || card == null)
            return 0;

        ShopItemData shopItem = FindShopItem(shop, card);
        if (shopItem != null)
            return shop.GetSellPrice(shopItem);

        return shop.GetSellPrice(card);
    }

    public static int GetBuyPrice(ShopData shop, int itemIndex)
    {
        return TryGetItem(shop, itemIndex, out ShopItemData item)
            ? shop.GetBuyPrice(item)
            : 0;
    }

    public static int GetRemainingStock(ShopData shop, int itemIndex)
    {
        if (!TryGetItem(shop, itemIndex, out ShopItemData item))
            return 0;

        if (!item.HasLimitedStock)
            return int.MaxValue;

        return Math.Max(0, item.Stock - GetPurchasedCount(shop.ShopKey, itemIndex));
    }

    public static int GetPurchasedCount(string shopKey, int itemIndex)
    {
        ShopProgressSaveData progress = GetProgress(shopKey, itemIndex);
        return progress != null ? progress.purchasedCount : 0;
    }

    public static ShopProgressSaveData[] ToArray()
    {
        ShopProgressSaveData[] result = new ShopProgressSaveData[Shops.Count];
        for (int i = 0; i < Shops.Count; i++)
        {
            result[i] = new ShopProgressSaveData
            {
                shopKey = Shops[i].shopKey,
                itemIndex = Shops[i].itemIndex,
                purchasedCount = Shops[i].purchasedCount
            };
        }

        return result;
    }

    public static void Restore(ShopProgressSaveData[] savedShops)
    {
        Shops.Clear();
        if (savedShops == null)
            return;

        for (int i = 0; i < savedShops.Length; i++)
        {
            ShopProgressSaveData savedShop = savedShops[i];
            if (savedShop == null || string.IsNullOrWhiteSpace(savedShop.shopKey) || savedShop.itemIndex < 0)
                continue;

            Shops.Add(new ShopProgressSaveData
            {
                shopKey = savedShop.shopKey,
                itemIndex = savedShop.itemIndex,
                purchasedCount = Math.Max(0, savedShop.purchasedCount)
            });
        }
    }

    public static void Clear()
    {
        Shops.Clear();
    }

    private static bool TryGetItem(ShopData shop, int itemIndex, out ShopItemData item)
    {
        item = null;
        if (shop == null || shop.Items == null || itemIndex < 0 || itemIndex >= shop.Items.Count)
            return false;

        item = shop.Items[itemIndex];
        return item != null && item.IsValid;
    }

    private static ShopItemData FindShopItem(ShopData shop, CardData card)
    {
        if (shop == null || card == null || shop.Items == null)
            return null;

        for (int i = 0; i < shop.Items.Count; i++)
        {
            ShopItemData item = shop.Items[i];
            if (item != null && item.card == card)
                return item;
        }

        return null;
    }

    private static ShopProgressSaveData GetOrCreateProgress(string shopKey, int itemIndex)
    {
        ShopProgressSaveData progress = GetProgress(shopKey, itemIndex);
        if (progress != null)
            return progress;

        progress = new ShopProgressSaveData
        {
            shopKey = shopKey,
            itemIndex = itemIndex,
            purchasedCount = 0
        };
        Shops.Add(progress);
        return progress;
    }

    private static ShopProgressSaveData GetProgress(string shopKey, int itemIndex)
    {
        if (string.IsNullOrWhiteSpace(shopKey) || itemIndex < 0)
            return null;

        for (int i = 0; i < Shops.Count; i++)
        {
            ShopProgressSaveData progress = Shops[i];
            if (progress != null && progress.shopKey == shopKey && progress.itemIndex == itemIndex)
                return progress;
        }

        return null;
    }
}
