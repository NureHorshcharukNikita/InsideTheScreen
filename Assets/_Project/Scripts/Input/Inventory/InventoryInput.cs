using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryInput : MonoBehaviour
{
    [SerializeField] private InventoryWindow inventoryWindow;

    // The quick keys of the window's pages.
    private static readonly (GameInputAction action, InventoryTab tab)[] TabKeys =
    {
        (GameInputAction.Inventory, InventoryTab.Inventory),
        (GameInputAction.Crafting, InventoryTab.Crafting),
        (GameInputAction.Quests, InventoryTab.Quests),
        (GameInputAction.Map, InventoryTab.Map),
        (GameInputAction.Skills, InventoryTab.Skills)
    };

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

            inventoryWindow.ToggleTab(tab);
            return;
        }
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
