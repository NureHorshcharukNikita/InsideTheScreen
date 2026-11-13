using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// The inventory window's keys, and the tabs and counters the story brings up (Story Unlock): when the
// story sets one's flag, a hint gives its key once the player has control again, e.g. "Press I to open
// your inventory". Once only: a flag that comes back with a save, or that an old save or the debug
// start catches up on as the scene starts, gives none; nor does one seen in the window already.
// Recipes Haru thinks of himself (CraftingProgress) come the same way: "Press C to make Stones".
public class InventoryInput : MonoBehaviour
{
    // How long the player has had control before a hint comes: the dialogue box is gone by then, and a
    // talk following right after another has begun.
    private const float RevealAfterControl = 0.4f;
    // How often Haru looks at what he carries for something new to make.
    private const float IdeasEvery = 0.5f;

    [SerializeField] private InventoryWindow inventoryWindow;

    // Flags the story has set while playing whose hints are still to come.
    private readonly HashSet<string> newFlags = new();
    private bool listening;
    private float controlSince = -1f;
    private float nextIdeas;
    private CraftingPageView craftingPage;

    // The quick keys of the window's pages.
    private static readonly (GameInputAction action, InventoryTab tab)[] TabKeys =
    {
        (GameInputAction.Inventory, InventoryTab.Inventory),
        (GameInputAction.Crafting, InventoryTab.Crafting),
        (GameInputAction.Quests, InventoryTab.Quests),
        (GameInputAction.Map, InventoryTab.Map),
        (GameInputAction.Skills, InventoryTab.Skills)
    };

    private void Awake()
    {
        StoryFlagManager.FlagSet += Remember;
    }

    private void OnDestroy()
    {
        StoryFlagManager.FlagSet -= Remember;
    }

    // From here on: what the scene starts with (a save, the debug start before this, an old save caught
    // up on) is no news.
    private void Start()
    {
        HudFlags.CatchUpOldSave();
        listening = true;
    }

    private void Remember(string flag)
    {
        if (listening)
            newFlags.Add(flag);
    }

    private void Update()
    {
        if (inventoryWindow == null)
            return;

        // Handled here because InventoryWindow is inactive while hidden.
        if (GameInputSettings.GetKeyDown(GameInputAction.Close))
        {
            if (inventoryWindow.IsOpen || inventoryWindow.IsPauseShown)
                inventoryWindow.HandleCloseInput();
            else if (GameStateManager.CanPause())
                inventoryWindow.PauseGame();

            return;
        }

        // Not over the pause menu (its settings take keys to rebind), and not while a letter is
        // typed into the card search.
        if (inventoryWindow.IsPauseShown || IsTyping())
            return;

        foreach (var (action, tab) in TabKeys)
        {
            if (!GameInputSettings.GetKeyDown(action))
                continue;

            // A tab the story has not brought up yet: its key does nothing.
            if (inventoryWindow.IsTabOpen(tab))
                inventoryWindow.ToggleTab(tab);

            return;
        }

        UpdateReveal();
    }

    private void UpdateReveal()
    {
        // Open, the window shows whatever is new in it: no hints for that.
        if (inventoryWindow.IsOpen)
        {
            newFlags.Clear();
            return;
        }

        if (!GameStateManager.CanOpenInventory() || GameStateManager.IsBattleScene || inventoryWindow.IsPauseShown)
        {
            controlSince = -1f;
            return;
        }

        if (controlSince < 0f)
            controlSince = Time.unscaledTime;

        if (Time.unscaledTime - controlSince < RevealAfterControl)
            return;

        if (newFlags.Count > 0)
        {
            RevealNew();
            return;
        }

        UpdateIdeas();
    }

    private void RevealNew()
    {
        // One hint at a time, the tabs first.
        foreach (var (action, tab) in TabKeys)
        {
            if (TryHint(inventoryWindow.GetTabUnlock(tab), action))
                return;
        }

        foreach (StoryUnlock unlock in inventoryWindow.CounterUnlocks)
        {
            if (TryHint(unlock, unlock != null ? unlock.HintKey : GameInputAction.Inventory))
                return;
        }

        // Flags no hint is for.
        newFlags.Clear();
    }

    // Something new to make out of what he carries: "Press C to make Stones", or "...and 2 more".
    private void UpdateIdeas()
    {
        if (Time.unscaledTime < nextIdeas || !inventoryWindow.IsTabOpen(InventoryTab.Crafting))
            return;

        nextIdeas = Time.unscaledTime + IdeasEvery;
        craftingPage ??= inventoryWindow.GetComponentInChildren<CraftingPageView>(true);
        if (craftingPage == null)
            return;

        List<CraftingRecipe> ideas = CraftingProgress.ThinkOfNew(craftingPage.Recipes, ExplorationPlayerSession.RuntimeInventory);
        if (ideas.Count == 0 || ideas[0].Result == null)
            return;

        string what = ideas[0].Result.CardName + (ideas.Count > 1 ? $" and {ideas.Count - 1} more" : string.Empty);
        QuestNotificationToast.ShowKeyHint(GameInputAction.Crafting, $"to make {what}");
    }

    // E.g. "Press I to view your health", if the story has just brought this up; a counter without
    // words gives none.
    private bool TryHint(StoryUnlock unlock, GameInputAction key)
    {
        if (unlock == null || string.IsNullOrWhiteSpace(unlock.Flag) || !newFlags.Remove(unlock.Flag))
            return false;

        if (string.IsNullOrWhiteSpace(unlock.KeyHint))
            return false;

        QuestNotificationToast.ShowKeyHint(key, unlock.KeyHint);
        return true;
    }

    private static bool IsTyping()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && selected.TryGetComponent(out TMP_InputField field) && field.isFocused;
    }

    // Switching to another window in the middle of play opens the pause menu. Not during a story
    // event or a dialogue: those wait where they are (GameClock) and go on when the game is back.
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && inventoryWindow != null && GameStateManager.CanPause())
            inventoryWindow.PauseGame();
    }
}
