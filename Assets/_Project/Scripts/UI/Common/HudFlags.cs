// The story flags that bring up parts of the HUD (Story Unlock on them in the inventory window), for
// code that shows the same things elsewhere: a quest's skill point or gold reward is not shown until
// the HUD shows skill points or gold. A tab or a counter is shown once the story has brought it up.
// Where a story moment already has a flag, it is that one (no ui_ flag beside it); ui_ flags are for
// the rest.
public static class HudFlags
{
    // The cards tab and the I key: Haru's first find turned into a card.
    public const string Inventory = "a1_first_gather_talk";
    // The crafting tab and the C key: Garr shows the salve at his bench.
    public const string Crafting = "a1_salve_recipe";
    // The skills tab and the skill points, both from Eldon.
    public const string Skills = "ui_skills";
    public const string Map = "ui_map";
    // Haru's health in the inventory: the stick test with Dad is over.
    public const string Health = "a1_health_test_done";
    public const string Mind = "ui_mind";
    // The gold in the inventory, and trading at all (shops wait for it): the trader pays for the salve,
    // Eldon's dust and his first coin.
    public const string Gold = "a1_crystal_dust_3";
    // The deck on the cards tab (its panel, the Add / Remove buttons): Dad, before the wolf, has Haru
    // pick what he takes into the fight. Until then nothing goes into the deck.
    public const string Deck = "a1_deck_lesson";

    public static bool IsOpen(string flag)
    {
        return StoryFlagManager.HasFlag(flag);
    }

    // A save from before the HUD came up bit by bit: what the story has already brought up is there,
    // without opening itself. Also puts right flags a story change has left behind. Once, as a scene
    // starts (InventoryInput).
    public static void CatchUpOldSave()
    {
        // From before the stick test: Survival Basics done without it.
        if (IsDone("a1_survival_basics"))
            Bring(Health);

        if (IsDone("a1_trader_salve") || IsDone("a1_heart_of_the_barrier"))
            Bring(Gold);

        if (IsDone("a1_heart_of_the_barrier"))
            Bring(Skills);

        // From before the salve: past Garr's bags, crafting is there all the same.
        if (StoryFlagManager.HasFlag("a1_bags_received"))
            Bring(Crafting);

        // From before Garr asked for the herbs first: he showed the salve straight away.
        if (StoryFlagManager.HasFlag("a1_salve_recipe"))
            Bring("a1_salve_ingredients");

        // From before his father's talk about making things: past the salve, Haru thinks of recipes.
        if (IsDone("a1_trader_salve") || StoryFlagManager.HasFlag("a1_bags_received"))
            Bring(CraftingProgress.IdeasFlag);

        // From before the deck waited for the wolf: past it, or with cards already in the deck.
        DeckData deck = ExplorationPlayerSession.RuntimeDeck;
        if (IsDone("a1_first_threat") || (deck != null && deck.Cards.Count > 0))
            Bring(Deck);

        // From before Standing on Your Own was taken out: Dad was left waiting for Haru to beat two
        // ants alone, and nothing would call him back now.
        if (StoryFlagManager.ClearFlag("a1_hideshi_waits"))
            StoryFlagManager.ClearFlag("hideshi_stays_behind");
    }

    private static void Bring(string flag)
    {
        StoryFlagManager.SetFlag(flag);
    }

    private static bool IsDone(string questKey)
    {
        return QuestManager.TryGetStatus(questKey, out QuestStatus status) && status == QuestStatus.Completed;
    }
}
