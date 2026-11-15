using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Opens the inventory window on one of its pages and waits until the player closes it, e.g. for Haru
// to pick what he takes into his first fight. While it is open the pointer shows the first of Point At
// that is there (a UiPointerTarget id), e.g. the Add to deck button once a card is picked, the cards
// before that; it goes once Pointer Until is met, e.g. a card is in the deck.
[Serializable, SubclassMenu("Story/Open Inventory")]
public sealed class OpenInventoryStep : StoryStep
{
    [SerializeField] private InventoryTab tab = InventoryTab.Inventory;
    [Tooltip("What the pointer shows, the first one there is: UiPointerTarget ids, e.g. deck_add, then inventory_cards. Empty = no pointer.")]
    [SerializeField] private List<string> pointAt = new();
    [SerializeField] private PointerSide pointerSide;
    [Tooltip("The pointer goes once these are met, e.g. In Deck (no card): something is in the deck. Empty = it stays while the window is open.")]
    [SerializeField] private List<StoryCondition> pointerUntil = new();

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
            if (pointerUntil.Count > 0 && StoryCondition.AreMet(pointerUntil))
                UiPointer.HideAll();
            else
                PointAtFirstThere();

            yield return null;
        }

        UiPointer.HideAll();
        GameStateManager.SetState(GameState.Dialogue);
    }

    private void PointAtFirstThere()
    {
        foreach (string id in pointAt)
        {
            if (UiPointer.TryPointAt(id, pointerSide))
                return;
        }

        UiPointer.HideAll();
    }

    public override string ToString()
    {
        return $"Open Inventory: {tab}";
    }
}
