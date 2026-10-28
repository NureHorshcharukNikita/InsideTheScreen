using System.Collections.Generic;

// What the crafting tab knows. A recipe shows up once any of its cards has been in the inventory,
// now or ever (a stone found, then used up, still taught what stones make). A recipe stays new,
// with a dot like an unread quest, until its row is clicked. Saved with the session.
public static class CraftingProgress
{
    private static readonly HashSet<string> ObtainedCards = new();
    private static readonly HashSet<string> OpenedRecipes = new();

    // Every card that comes into an inventory (InventoryData.AddCards).
    public static void NoteObtained(CardData card)
    {
        if (card != null && !string.IsNullOrEmpty(card.CardID) && ObtainedCards.Add(card.CardID))
            ExplorationPlayerSession.SaveCraftingProgress();
    }

    public static bool IsKnown(CraftingRecipe recipe, InventoryData inventory)
    {
        if (recipe == null)
            return false;

        foreach (CraftingRecipe.Ingredient ingredient in recipe.Ingredients)
        {
            if (ingredient != null && (WasObtained(ingredient.card, inventory) || WasObtained(ingredient.orInstead, inventory)))
                return true;
        }

        return false;
    }

    public static bool IsNew(CraftingRecipe recipe, InventoryData inventory)
    {
        return IsKnown(recipe, inventory) && !OpenedRecipes.Contains(recipe.name);
    }

    public static void MarkOpened(CraftingRecipe recipe)
    {
        if (recipe != null && OpenedRecipes.Add(recipe.name))
            ExplorationPlayerSession.SaveCraftingProgress();
    }

    public static string[] ObtainedToArray()
    {
        return new List<string>(ObtainedCards).ToArray();
    }

    public static string[] OpenedToArray()
    {
        return new List<string>(OpenedRecipes).ToArray();
    }

    // Saves made before crafting have neither: then only the cards in the inventory count.
    public static void Restore(string[] obtainedCardIds, string[] openedRecipeKeys)
    {
        Clear();
        if (obtainedCardIds != null)
            ObtainedCards.UnionWith(obtainedCardIds);
        if (openedRecipeKeys != null)
            OpenedRecipes.UnionWith(openedRecipeKeys);
    }

    public static void Clear()
    {
        ObtainedCards.Clear();
        OpenedRecipes.Clear();
    }

    private static bool WasObtained(CardData card, InventoryData inventory)
    {
        return card != null
            && (ObtainedCards.Contains(card.CardID) || Crafting.Owned(inventory, card) > 0);
    }
}
