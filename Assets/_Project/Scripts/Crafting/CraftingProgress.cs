using System.Collections.Generic;

// What the crafting tab knows. A recipe someone teaches (Learned At) is known once the story says so,
// e.g. Garr's salve. The others Haru thinks of himself, but only after his father's talk about making
// things (Ideas Flag): then a recipe comes to him once he has all it takes at once, and stays known
// when he uses it up. A recipe stays new, with a dot like an unread quest, until its row is clicked.
// Saved with the session.
public static class CraftingProgress
{
    // His father's talk after the salve: there is more in what they carry than they'd think.
    public const string IdeasFlag = "a1_craft_ideas";

    private static readonly HashSet<string> ObtainedCards = new();
    private static readonly HashSet<string> OpenedRecipes = new();
    private static readonly HashSet<string> ThoughtOf = new();

    // Every card that comes into an inventory (InventoryData.AddCards).
    public static void NoteObtained(CardData card)
    {
        if (card != null && !string.IsNullOrEmpty(card.CardID) && ObtainedCards.Add(card.CardID))
            ExplorationPlayerSession.SaveCraftingProgress();
    }

    // A recipe opened before (in an older save) stays known.
    public static bool IsKnown(CraftingRecipe recipe, InventoryData inventory)
    {
        if (recipe == null)
            return false;

        if (!string.IsNullOrWhiteSpace(recipe.LearnedAt))
            return StoryFlagManager.HasFlag(recipe.LearnedAt);

        return ThoughtOf.Contains(recipe.name) || OpenedRecipes.Contains(recipe.name);
    }

    // The recipes Haru thinks of now: nobody teaches them, and he has all they take at once. From then
    // on they are known.
    public static List<CraftingRecipe> ThinkOfNew(IEnumerable<CraftingRecipe> recipes, InventoryData inventory)
    {
        List<CraftingRecipe> found = new();
        if (recipes == null || inventory == null || !StoryFlagManager.HasFlag(IdeasFlag))
            return found;

        foreach (CraftingRecipe recipe in recipes)
        {
            if (recipe == null || !string.IsNullOrWhiteSpace(recipe.LearnedAt) || IsKnown(recipe, inventory)
                || Crafting.HowManyTimes(inventory, recipe) <= 0)
                continue;

            ThoughtOf.Add(recipe.name);
            found.Add(recipe);
        }

        if (found.Count > 0)
            ExplorationPlayerSession.SaveCraftingProgress();

        return found;
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

    public static string[] ThoughtOfToArray()
    {
        return new List<string>(ThoughtOf).ToArray();
    }

    public static void Restore(string[] obtainedCardIds, string[] openedRecipeKeys, string[] thoughtOfRecipeKeys)
    {
        Clear();
        if (obtainedCardIds != null)
            ObtainedCards.UnionWith(obtainedCardIds);
        if (openedRecipeKeys != null)
            OpenedRecipes.UnionWith(openedRecipeKeys);
        if (thoughtOfRecipeKeys != null)
            ThoughtOf.UnionWith(thoughtOfRecipeKeys);
    }

    public static void Clear()
    {
        ObtainedCards.Clear();
        OpenedRecipes.Clear();
        ThoughtOf.Clear();
    }
}
