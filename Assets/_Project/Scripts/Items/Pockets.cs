using System.Collections.Generic;
using UnityEngine;

// What Haru can carry before he has his bags (until Bags Flag, when Garr gives them): a few
// pockets, each for one kind of thing, as many of it as he finds. Something that would need one
// more pocket stays where it is and its prompt says so (ForagePickup); a chest does not open while
// nothing in it would fit, and gives only what fits (LootChest). Story things such as Crystal Dust
// take no pocket. Kept in Resources, like the Pickup Card, so every pickup finds it without holding
// a reference; without it Haru carries anything.
[CreateAssetMenu(menuName = "Game/Items/Pockets", fileName = ResourceName)]
public sealed class Pockets : ScriptableObject
{
    public const string ResourceName = "Pockets";

    [Tooltip("How many kinds of things Haru carries until Bags Flag is set.")]
    [SerializeField, Min(1)] private int count = 10;
    [Tooltip("Set once Haru has his bags: no limit from then on. Empty = never limited.")]
    [SerializeField, StoryFlag] private string bagsFlag = "a1_bags_worn";
    [Tooltip("Set the first time all pockets are full, for a story event to follow it. Empty = none.")]
    [SerializeField, StoryFlag] private string filledFlag;
    [Tooltip("Story things that take no pocket, e.g. Crystal Dust for Eldon.")]
    [SerializeField] private List<CardData> takeNoPocket = new();
    [Tooltip("Shown on the prompt when something would need another pocket.")]
    [SerializeField] private string fullMessage = "My pockets are full.";

    private static Pockets instance;

    private static Pockets Instance =>
        instance != null ? instance : instance = Resources.Load<Pockets>(ResourceName);

    public static bool AreLimited
    {
        get
        {
            Pockets pockets = Instance;
            return pockets != null && !string.IsNullOrWhiteSpace(pockets.bagsFlag)
                && !StoryFlagManager.HasFlag(pockets.bagsFlag);
        }
    }

    public static int Count => Instance != null ? Instance.count : 0;

    public static string FullMessage => Instance != null ? Instance.fullMessage : string.Empty;

    // The pockets in use: the kinds of things in the inventory that take one.
    public static int Used(InventoryData inventory)
    {
        if (inventory == null)
            return 0;

        HashSet<CardData> kinds = new();
        foreach (CardData card in inventory.Cards)
        {
            if (TakesPocket(card))
                kinds.Add(card);
        }

        return kinds.Count;
    }

    // Whether Haru has room for it, besides the kinds he is taking along with it (a chest's cards).
    public static bool CanTake(InventoryData inventory, CardData card, ICollection<CardData> alsoTaking = null)
    {
        if (!AreLimited || inventory == null || !TakesPocket(card) || Holds(inventory, card)
            || (alsoTaking != null && alsoTaking.Contains(card)))
            return true;

        int used = Used(inventory);
        if (alsoTaking != null)
        {
            foreach (CardData taking in alsoTaking)
            {
                if (TakesPocket(taking) && !Holds(inventory, taking))
                    used++;
            }
        }

        return used < Count;
    }

    // After something went into the inventory: the Filled Flag once the last pocket is taken.
    public static void NoteTaken(InventoryData inventory)
    {
        Pockets pockets = Instance;
        if (pockets != null && AreLimited && !string.IsNullOrWhiteSpace(pockets.filledFlag)
            && Used(inventory) >= pockets.count)
            StoryFlagManager.SetFlag(pockets.filledFlag);
    }

    private static bool TakesPocket(CardData card)
    {
        Pockets pockets = Instance;
        return card != null && pockets != null && !pockets.takeNoPocket.Contains(card);
    }

    private static bool Holds(InventoryData inventory, CardData card)
    {
        foreach (CardData held in inventory.Cards)
        {
            if (held == card)
                return true;
        }

        return false;
    }
}
