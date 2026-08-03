using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class CardView
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsBattleInteractionLocked())
            return;

        if (TryForwardInventoryDrag(eventData, (handler, pointerEventData) => handler.OnBeginDrag(pointerEventData)))
            return;

        battleDrag.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsBattleInteractionLocked())
        {
            ForceReleaseBattleDragToHand();
            return;
        }

        if (TryForwardInventoryDrag(eventData, (handler, pointerEventData) => handler.OnDrag(pointerEventData)))
            return;

        battleDrag.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsBattleInteractionLocked())
        {
            ForceReleaseBattleDragToHand();
            return;
        }

        if (TryForwardInventoryDrag(eventData, (handler, pointerEventData) => handler.OnEndDrag(pointerEventData)))
            return;

        battleDrag.OnEndDrag(eventData);
    }

    private bool TryForwardInventoryDrag(PointerEventData eventData, Action<ScrollRect, PointerEventData> forward)
    {
        if (battleDrag.IsBattleDragEnabled)
            return false;

        ScrollRect scroll = GetComponentInParent<ScrollRect>();
        if (scroll != null)
            forward(scroll, eventData);

        return true;
    }
}
