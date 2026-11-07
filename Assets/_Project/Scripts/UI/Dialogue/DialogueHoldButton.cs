using System;
using UnityEngine;
using UnityEngine.EventSystems;

// A dialogue box arrow that acts while the mouse holds it down, the way its key does (DialogueView).
// It answers the press itself, not the click, so a long hold does not end in one more click.
public sealed class DialogueHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public event Action Pressed;

    public bool IsHeld { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        IsHeld = true;
        Pressed?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            IsHeld = false;
    }

    // Hidden while a choice is up: the hold ends with it.
    private void OnDisable()
    {
        IsHeld = false;
    }
}
