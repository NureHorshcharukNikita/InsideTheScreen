using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// The inventory window's keys, and the tabs the story brings up (Story Unlock): once the player has
// control again, a hint gives the new tab's quick key, e.g. "Press I to open your inventory" - right
// away, or, for a tab that opens the window by itself (Opens Window), once that first look is over.
public class InventoryInput : MonoBehaviour
{
    // How long the player has had control before a new tab opens: the dialogue box is gone by then,
    // and a talk following right after another has begun.
    private const float RevealAfterControl = 0.4f;

    [SerializeField] private InventoryWindow inventoryWindow;

    // What the window opened by itself to show (Opens Window); its hint comes once the window closes.
    private StoryUnlock revealing;
    private GameInputAction revealingKey;
    private float controlSince = -1f;

    // The quick keys of the window's pages.
    private static readonly (GameInputAction action, InventoryTab tab)[] TabKeys =
    {
        (GameInputAction.Inventory, InventoryTab.Inventory),
        (GameInputAction.Crafting, InventoryTab.Crafting),
        (GameInputAction.Quests, InventoryTab.Quests),
        (GameInputAction.Map, InventoryTab.Map),
        (GameInputAction.Skills, InventoryTab.Skills)
    };

    private void Start()
    {
        HudFlags.CatchUpOldSave();
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
        if (revealing != null)
        {
            if (inventoryWindow.IsOpen)
                return;

            Hint(revealing, revealingKey);
            revealing = null;
        }

        if (!GameStateManager.CanOpenInventory() || GameStateManager.IsBattleScene || inventoryWindow.IsOpen
            || inventoryWindow.IsPauseShown)
        {
            controlSince = -1f;
            return;
        }

        if (controlSince < 0f)
            controlSince = Time.unscaledTime;

        if (Time.unscaledTime - controlSince < RevealAfterControl)
            return;

        foreach (var (action, tab) in TabKeys)
        {
            StoryUnlock unlock = inventoryWindow.GetTabUnlock(tab);
            if (unlock == null || !unlock.IsNew)
                continue;

            if (unlock.OpensWindow)
                inventoryWindow.ToggleTab(tab);

            Reveal(unlock, action);
            return;
        }

        foreach (StoryUnlock unlock in inventoryWindow.CounterUnlocks)
        {
            if (unlock == null || !unlock.IsNew)
                continue;

            if (unlock.OpensWindow)
                inventoryWindow.Open();

            Reveal(unlock, unlock.HintKey);
            return;
        }
    }

    // The hint now, or once the window it opened is closed again.
    private void Reveal(StoryUnlock unlock, GameInputAction key)
    {
        if (unlock.OpensWindow && inventoryWindow.IsOpen)
        {
            revealing = unlock;
            revealingKey = key;
            return;
        }

        Hint(unlock, key);
    }

    // E.g. "Press I to view your health"; a counter without words is just marked as shown.
    private static void Hint(StoryUnlock unlock, GameInputAction key)
    {
        unlock.MarkShown();
        QuestNotificationToast.ShowKeyHint(key, unlock.KeyHint);
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
