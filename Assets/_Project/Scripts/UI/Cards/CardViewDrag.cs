using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class CardView
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (TryForwardInventoryDrag(eventData, (s, e) => s.OnBeginDrag(e)))
            return;

        battleDrag.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (TryForwardInventoryDrag(eventData, (s, e) => s.OnDrag(e)))
            return;

        battleDrag.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (TryForwardInventoryDrag(eventData, (s, e) => s.OnEndDrag(e)))
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
