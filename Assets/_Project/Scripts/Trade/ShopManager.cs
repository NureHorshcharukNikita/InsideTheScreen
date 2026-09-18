using System;
using System.Collections.Generic;

public static class ShopManager
{
    private static readonly List<ShopProgressSaveData> Shops = new();

    public static event Action<ShopData, ShopItemData> ItemPurchased;
    public static event Action<ShopData, CardData, int> ItemSold;

    public static bool Buy(ShopData shop, int itemIndex, InventoryData inventory)
    {
        return Buy(shop, itemIndex, inventory, 1) == 1;
    }

    public static int Buy(ShopData shop, int itemIndex, InventoryData inventory, int quantity)
    {
        if (!TryGetItem(shop, itemIndex, out ShopItemData item) || inventory == null || quantity <= 0)
            return 0;

        int remainingStock = GetRemainingStock(shop, itemIndex);
        int price = GetBuyPrice(shop, itemIndex);
        int affordableQuantity = price > 0 ? PlayerWallet.Gold / price : quantity;
        int purchasedCount = Math.Min(quantity, Math.Min(remainingStock, affordableQuantity));
        if (purchasedCount <= 0)
            return 0;

        if (!PlayerWallet.TrySpendGold(price * purchasedCount))
            return 0;

        inventory.AddCards(item.card, purchasedCount);

        if (item.HasLimitedStock)
        {
            ShopProgressSaveData progress = GetOrCreateProgress(shop.ShopKey, itemIndex);
            progress.purchasedCount += purchasedCount;
        }

        ItemPurchased?.Invoke(shop, item);
        ExplorationPlayerSession.SaveShopProgress();
        return purchasedCount;
    }

    public static bool Sell(ShopData shop, CardData card, InventoryData inventory)
    {
        return Sell(shop, card, inventory, 1) == 1;
    }

    public static int Sell(ShopData shop, CardData card, InventoryData inventory, int quantity)
    {
        if (shop == null || card == null || inventory == null || quantity <= 0)
            return 0;

        int price = GetSellPrice(shop, card);
        if (price <= 0)
            return 0;

        int soldCount = inventory.RemoveCards(card, quantity);
        if (soldCount <= 0)
            return 0;

        int totalPrice = price * soldCount;
        PlayerWallet.AddGold(totalPrice);
        ItemSold?.Invoke(shop, card, totalPrice);
        return soldCount;
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
