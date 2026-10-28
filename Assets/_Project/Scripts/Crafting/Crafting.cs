using System;
using UnityEngine;

// Crafting with the cards in the inventory (CraftingRecipe): how many times a recipe can be made,
// and making it. The ingredients leave the inventory (and the deck, if it held more than is
// left), the made cards come into the inventory; Haru puts them in the deck himself.
public static class Crafting
{
    // More at once than anyone would want; also keeps a recipe made only of tools finite.
    public const int MaxAtOnce = 99;

    public static event Action<CraftingRecipe, int> Crafted;

    public static int Owned(InventoryData inventory, CardData card)
    {
        if (inventory == null || card == null)
            return 0;

        int count = 0;
        foreach (CardData owned in inventory.Cards)
        {
            if (owned == card)
                count++;
        }

        return count;
    }

    // The card an ingredient is made with: the first one, unless only the other one is at hand.
    public static CardData Choose(InventoryData inventory, CraftingRecipe.Ingredient ingredient, int times = 1)
    {
        if (ingredient == null)
            return null;

        if (ingredient.orInstead == null || Has(inventory, ingredient.card, ingredient.count, ingredient.tool, times))
            return ingredient.card;

        return Has(inventory, ingredient.orInstead, ingredient.count, ingredient.orInsteadIsTool, times)
            ? ingredient.orInstead
            : ingredient.card;
    }

    public static bool IsTool(CraftingRecipe.Ingredient ingredient, CardData chosen)
    {
        return ingredient != null && (chosen == ingredient.orInstead && ingredient.orInstead != null
            ? ingredient.orInsteadIsTool
            : ingredient.tool);
    }

    // How many cards of it a craft of this many times takes: a tool is needed only once.
    public static int Needed(CraftingRecipe.Ingredient ingredient, CardData chosen, int times)
    {
        if (ingredient == null)
            return 0;

        return IsTool(ingredient, chosen) ? ingredient.count : ingredient.count * Mathf.Max(1, times);
    }

    public static int HowManyTimes(InventoryData inventory, CraftingRecipe recipe)
    {
        if (recipe == null || recipe.Result == null)
            return 0;

        int times = MaxAtOnce;
        foreach (CraftingRecipe.Ingredient ingredient in recipe.Ingredients)
        {
            if (ingredient?.card == null)
                continue;

            times = Mathf.Min(times, Mathf.Max(
                TimesWith(inventory, ingredient.card, ingredient.count, ingredient.tool),
                TimesWith(inventory, ingredient.orInstead, ingredient.count, ingredient.orInsteadIsTool)));
        }

        return times;
    }

    public static bool CanCraft(InventoryData inventory, CraftingRecipe recipe, int times = 1)
    {
        return times >= 1 && HowManyTimes(inventory, recipe) >= times;
    }

    public static bool Craft(InventoryData inventory, CraftingRecipe recipe, int times)
    {
        if (inventory == null || !CanCraft(inventory, recipe, times))
            return false;

        foreach (CraftingRecipe.Ingredient ingredient in recipe.Ingredients)
        {
            if (ingredient?.card == null)
                continue;

            CardData chosen = Choose(inventory, ingredient, times);
            if (!IsTool(ingredient, chosen))
                inventory.RemoveCards(chosen, Needed(ingredient, chosen, times));
        }

        ExplorationPlayerSession.TrimDeckToInventory();

        int made = recipe.ResultCount * times;
        inventory.AddCards(recipe.Result, made);
        Crafted?.Invoke(recipe, times);
        return true;
    }

    private static bool Has(InventoryData inventory, CardData card, int count, bool tool, int times)
    {
        return card != null && Owned(inventory, card) >= (tool ? count : count * Mathf.Max(1, times));
    }

    private static int TimesWith(InventoryData inventory, CardData card, int count, bool tool)
    {
        if (card == null)
            return 0;

        int owned = Owned(inventory, card);
        if (tool)
            return owned >= count ? MaxAtOnce : 0;

        return owned / Mathf.Max(1, count);
    }
}
