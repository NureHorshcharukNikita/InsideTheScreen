using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Opens the inventory window on one of its pages and waits until the player closes it, e.g. for Haru
// to pick what he takes into his first fight. While it is open the pointer shows the first of its
// hints that is on screen and not done yet, with what to do said past the arrow, e.g. "Pick a card"
// at the cards, then "Press Add" at the Add to deck button once a card is picked, then the close
// button once something is in the deck.
[Serializable, SubclassMenu("Story/Open Inventory")]
public sealed class OpenInventoryStep : StoryStep
{
    [Serializable]
    public sealed class PointerHint
    {
        [Tooltip("What the pointer shows: a UiPointerTarget id, e.g. inventory_cards, deck_add, inventory_close.")]
        public string target;
        public PointerSide side;
        [Tooltip("Said in a box past the arrow, e.g. \"Pick a card to take into the fight.\" Empty = the arrow alone.")]
        [TextArea(1, 3)] public string caption;
        [Tooltip("Not shown any more once these are met, e.g. In Deck (no card): something is in the deck. Empty = shown while it is on screen.")]
        public List<StoryCondition> doneWhen = new();
    }

    [SerializeField] private InventoryTab tab = InventoryTab.Inventory;
    [Tooltip("The first of these that is on screen and not done is shown; none = no pointer.")]
    [SerializeField] private List<PointerHint> hints = new();

    public override IEnumerator Run(StoryEventContext context)
    {
        InventoryWindow window = UnityEngine.Object.FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);
        if (window == null || !window.IsTabOpen(tab))
            yield break;

        // The window opens only in play; the event takes control back once it is closed.
        GameStateManager.SetState(GameState.Gameplay);
        window.ToggleTab(tab);

        while (window.IsOpen)
        {
            ShowFirstHint();
            yield return null;
        }

        UiPointer.HideAll();
        GameStateManager.SetState(GameState.Dialogue);
    }

    private void ShowFirstHint()
    {
        foreach (PointerHint hint in hints)
        {
            if (hint == null || (hint.doneWhen != null && hint.doneWhen.Count > 0 && StoryCondition.AreMet(hint.doneWhen)))
                continue;

            if (UiPointer.TryPointAt(hint.target, hint.side, hint.caption))
                return;
        }

        UiPointer.HideAll();
    }

    public override string ToString()
    {
        return $"Open Inventory: {tab}";
    }
}
