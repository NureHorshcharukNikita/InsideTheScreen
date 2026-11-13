// The story flags that bring up parts of the HUD (Story Unlock on them in the inventory window), for
// code that shows the same things elsewhere: a quest's skill point or gold reward is not shown until
// the HUD shows skill points or gold. A tab or a counter is shown once the story has brought it up.
public static class HudFlags
{
    public const string Inventory = "ui_inventory";
    public const string Crafting = "ui_crafting";
    public const string Skills = "ui_skills";
    public const string Map = "ui_map";
    public const string Health = "ui_health";
    public const string Mind = "ui_mind";
    public const string SkillPoints = "ui_skill_points";
    public const string Gold = "ui_gold";
    // Not a part of the HUD: trading at all, the trader first (him in Leymar and at his camp).
    public const string Trade = "ui_trade";

    public static bool IsOpen(string flag)
    {
        return StoryFlagManager.HasFlag(flag);
    }

    // A save from before the HUD came up bit by bit: what the story has already brought up is there,
    // without opening itself. Once, as a scene starts (InventoryInput).
    public static void CatchUpOldSave()
    {
        if (StoryFlagManager.HasFlag("a1_first_gather_talk"))
            Bring(Inventory, true);

        if (IsDone("a1_survival_basics"))
            Bring(Health, true);

        if (IsDone("a1_trader_salve") || IsDone("a1_heart_of_the_barrier"))
        {
            Bring(Gold, true);
            Bring(Trade, true);
        }

        if (IsDone("a1_heart_of_the_barrier"))
        {
            Bring(Skills, true);
            Bring(SkillPoints, true);
        }

        if (StoryFlagManager.HasFlag("a1_salve_recipe") || StoryFlagManager.HasFlag("a1_bags_received"))
            Bring(Crafting, true);
    }

    private static void Bring(string flag, bool shown)
    {
        if (StoryFlagManager.HasFlag(flag))
            return;

        StoryFlagManager.SetFlag(flag);
        if (shown)
            StoryFlagManager.SetFlag(flag + "_shown");
    }

    private static bool IsDone(string questKey)
    {
        return QuestManager.TryGetStatus(questKey, out QuestStatus status) && status == QuestStatus.Completed;
    }
}
