using System;
using UnityEngine;

public static class PlayerWallet
{
    private static int gold;
    private static bool initialized;

    public static int Gold => gold;
    public static bool IsInitialized => initialized;

    public static event Action<int> GoldChanged;

    public static void Initialize(int startingGold)
    {
        if (initialized)
            return;

        initialized = true;
        gold = Mathf.Max(0, startingGold);
        GoldChanged?.Invoke(gold);
        ExplorationPlayerSession.SaveWallet();
    }

    public static void Restore(bool hasGold, int savedGold)
    {
        initialized = hasGold;
        gold = hasGold ? Mathf.Max(0, savedGold) : 0;
        GoldChanged?.Invoke(gold);
    }

    public static void Clear()
    {
        initialized = false;
        gold = 0;
        GoldChanged?.Invoke(gold);
    }

    public static void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        EnsureInitialized();
        gold += amount;
        GoldChanged?.Invoke(gold);
        ExplorationPlayerSession.SaveWallet();
    }

    public static bool TrySpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        EnsureInitialized();
        if (gold < amount)
            return false;

        gold -= amount;
        GoldChanged?.Invoke(gold);
        ExplorationPlayerSession.SaveWallet();
        return true;
    }

    private static void EnsureInitialized()
    {
        if (!initialized)
            Initialize(0);
    }
}
