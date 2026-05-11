using UnityEngine;
using UnityEngine.UI;

public sealed partial class CardBattleDragHandler
{
    public void ForceReleaseDragToHand()
    {
        CancelReturnAnimationIfRunning();

        if (rectTransform == null || canvasRootRect == null || handParent == null)
            return;

        if (rectTransform.parent == canvasRootRect)
            RestoreCachedHandSlot();

        SetRaycastBlocking(true);
    }

    public void SyncHierarchy(
        RectTransform rect,
        Transform hand,
        RectTransform canvasRoot,
        Canvas root,
        LayoutElement layout,
        CanvasGroup group)
    {
        rectTransform = rect;
        handParent = hand;
        canvasRootRect = canvasRoot;
        rootCanvas = root;
        layoutElement = layout;
        canvasGroup = group;
    }

    private bool HasValidDragContext()
    {
        return rectTransform != null && canvasRootRect != null && handParent != null;
    }

    private void CancelReturnAnimationIfRunning()
    {
        if (returnCoroutine == null)
            return;

        owner.StopCoroutine(returnCoroutine);
        returnCoroutine = null;

        RestoreCachedHandSlot();
    }

    private void CacheHandSlotBeforeDrag()
    {
        handAnchoredPosition = rectTransform.anchoredPosition;
        handWorldPosition = rectTransform.position;
        handSiblingIndex = rectTransform.GetSiblingIndex();
    }

    private void RestoreCachedHandSlot()
    {
        if (rectTransform == null || handParent == null)
            return;

        if (rectTransform.parent != handParent)
            rectTransform.SetParent(handParent, worldPositionStays: true);

        rectTransform.anchoredPosition = handAnchoredPosition;
        rectTransform.SetSiblingIndex(ClampSiblingIndex(handSiblingIndex));
        SetLayoutIgnoredForDrag(false);
        SetRaycastBlocking(true);
    }

    private int ClampSiblingIndex(int index)
    {
        int max = Mathf.Max(0, handParent.childCount - 1);
        return Mathf.Clamp(index, 0, max);
    }

    private void SetLayoutIgnoredForDrag(bool ignored)
    {
        if (layoutElement != null)
            layoutElement.ignoreLayout = ignored;
    }

    private void MoveUnderRootCanvasUnclipped()
    {
        rectTransform.SetParent(canvasRootRect, worldPositionStays: true);
        rectTransform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
    }

    private void SetRaycastBlocking(bool block)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = block;
    }
}
