using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Darkens a button's icons while the pointer is over it. For a button that is only a clear hit
// area with its icons as children (the dialogue's continue and skip buttons): the Button's own
// tint reaches its target graphic only. Tints through the CanvasRenderer, so the colours set on
// the icons stay as they are.
[DisallowMultipleComponent]
public sealed class HoverDarken : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static readonly Color HoverTint = new(0.7f, 0.7f, 0.7f, 1f);

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetDarkened(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetDarkened(false);
    }

    // Hidden under the pointer (choices shown, dialogue closed): no pointer exit comes then.
    private void OnDisable()
    {
        SetDarkened(false);
    }

    // Inactive icons included: the continue button swaps its arrow while the text is typing.
    private void SetDarkened(bool darkened)
    {
        Color tint = darkened ? HoverTint : Color.white;
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            graphic.canvasRenderer.SetColor(tint);
    }
}
