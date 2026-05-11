using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CardBattleDragHandler
{
    private readonly CardView owner;

    private BattleSystem battleSystem;
    private float returnAnimationDuration;

    private RectTransform rectTransform;
    private RectTransform canvasRootRect;
    private Canvas rootCanvas;
    private Transform handParent;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;

    private Vector2 handAnchoredPosition;
    private Vector3 handWorldPosition;
    private int handSiblingIndex;
    private Vector2 dragPointerOffset;
    private Coroutine returnCoroutine;

    public CardBattleDragHandler(CardView owner)
    {
        this.owner = owner;
    }

    public bool IsBattleDragEnabled => battleSystem != null;

    public void Configure(BattleSystem battle, float returnDuration)
    {
        battleSystem = battle;
        returnAnimationDuration = returnDuration;
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsBattleDragEnabled)
            return;

        if (!CanBeginBattleDrag(eventData))
            return;

        CancelReturnAnimationIfRunning();
        CacheHandSlotBeforeDrag();

        MoveUnderRootCanvasUnclipped();
        SetLayoutIgnoredForDrag(true);

        CacheDragPointerOffset(eventData);
        SetRaycastBlocking(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsBattleDragEnabled || !HasValidDragContext())
            return;

        if (!TryScreenToCanvasLocal(eventData.position, out Vector2 localPoint))
            return;

        rectTransform.anchoredPosition = localPoint + dragPointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsBattleDragEnabled)
            return;

        if (!HasValidDragContext())
        {
            SetRaycastBlocking(true);
            return;
        }

        if (TryPlayOnDropTarget(eventData.position))
        {
            SetLayoutIgnoredForDrag(false);
            SetRaycastBlocking(true);
            Object.Destroy(owner.gameObject);
            return;
        }

        SetRaycastBlocking(false);
        returnCoroutine = owner.StartCoroutine(AnimateReturnToHandCoroutine());
    }

    private bool CanBeginBattleDrag(PointerEventData eventData)
    {
        return eventData.button == PointerEventData.InputButton.Left && HasValidDragContext();
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

    private void CacheDragPointerOffset(PointerEventData eventData)
    {
        if (!TryScreenToCanvasLocal(eventData.position, out Vector2 pressLocal))
            pressLocal = rectTransform.anchoredPosition;

        dragPointerOffset = rectTransform.anchoredPosition - pressLocal;
    }

    private void SetRaycastBlocking(bool block)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = block;
    }

    private bool TryScreenToCanvasLocal(Vector2 screenPosition, out Vector2 localPoint)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRootRect,
            screenPosition,
            GetUiEventCamera(),
            out localPoint);
    }

    private Camera GetUiEventCamera()
    {
        if (rootCanvas == null)
            return null;

        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
    }

    private bool TryPlayOnDropTarget(Vector2 screenPosition)
    {
        IEffectTarget target = ResolveDropTarget(screenPosition);
        return target != null && battleSystem.TryPlayCardFromHand(owner.CardIndex, target);
    }

    private IEnumerator AnimateReturnToHandCoroutine()
    {
        try
        {
            if (rectTransform == null || handParent == null)
                yield break;

            // Keep card under root canvas for the whole return tween to avoid clipping
            // and local-space jumps when reparenting too early.
            Vector3 from = rectTransform.position;
            Vector3 to = handWorldPosition;
            float duration = Mathf.Max(1e-4f, returnAnimationDuration);

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float eased = EaseOutCubic(Mathf.Clamp01(t));
                rectTransform.position = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            rectTransform.position = to;
            RestoreCachedHandSlot();
        }
        finally
        {
            returnCoroutine = null;
        }
    }

    private static float EaseOutCubic(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }

    private static IEffectTarget ResolveDropTarget(Vector2 screenPosition)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return null;

        Vector3 world = camera.ScreenToWorldPoint(screenPosition);
        world.z = 0f;

        Collider2D hit = Physics2D.OverlapPoint(world);
        return hit != null ? hit.GetComponentInParent<IEffectTarget>() : null;
    }
}
