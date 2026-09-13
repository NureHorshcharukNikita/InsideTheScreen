using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class CardView
{
    private static readonly List<RaycastResult> InventoryDropRaycastResults = new();

    private bool inventoryTransferDragging;
    private Vector3 inventoryDragWorldPosition;
    private Vector2 inventoryDragPointerOffset;
    private RectTransform inventoryDragGhost;
    private Coroutine inventoryReturnCoroutine;
    private bool suppressInventoryClickAfterDrag;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsBattleInteractionLocked())
            return;

        if (TryBeginInventoryTransferDrag(eventData))
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

        if (inventoryTransferDragging)
        {
            DragInventoryTransfer(eventData);
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

        if (inventoryTransferDragging)
        {
            EndInventoryTransferDrag(eventData);
            return;
        }

        if (TryForwardInventoryDrag(eventData, (handler, pointerEventData) => handler.OnEndDrag(pointerEventData)))
            return;

        battleDrag.OnEndDrag(eventData);
    }

    private bool TryForwardInventoryDrag(PointerEventData eventData, Action<ScrollRect, PointerEventData> forward)
    {
        if (battleDrag != null && battleDrag.IsBattleDragEnabled)
            return false;

        if (CanUseInventoryTransferDrag())
            return false;

        ScrollRect scroll = GetComponentInParent<ScrollRect>();
        if (scroll != null)
            forward(scroll, eventData);

        return true;
    }

    private bool TryBeginInventoryTransferDrag(PointerEventData eventData)
    {
        if (!CanUseInventoryTransferDrag())
            return false;

        if (eventData.button != PointerEventData.InputButton.Left)
            return false;

        if (!HasValidInventoryDragContext())
            return false;

        CancelInventoryReturnAnimationIfRunning();
        CacheInventorySlotBeforeDrag();
        inventoryTransferDragging = true;

        CreateInventoryDragGhost();
        CacheInventoryDragPointerOffset(eventData);
        return true;
    }

    private void DragInventoryTransfer(PointerEventData eventData)
    {
        if (!HasValidInventoryDragContext())
        {
            ForceReleaseInventoryTransferDrag(suppressClick: true);
            return;
        }

        if (!TryScreenToInventoryCanvasLocal(eventData.position, out Vector2 localPoint))
            return;

        RectTransform dragRect = GetInventoryDragVisualRect();
        if (dragRect != null)
            dragRect.anchoredPosition = localPoint + inventoryDragPointerOffset;
    }

    private void EndInventoryTransferDrag(PointerEventData eventData)
    {
        inventoryTransferDragging = false;
        suppressInventoryClickAfterDrag = true;

        if (eventData == null)
        {
            inventoryReturnCoroutine = StartCoroutine(AnimateInventoryReturnCoroutine());
            return;
        }

        InventoryCardLocation target = ResolveInventoryDropTarget(eventData);
        if (target != InventoryCardLocation.None && target != inventoryDragSource)
        {
            RestoreInventoryDragSlot();
            onInventoryDrop?.Invoke(cardData, inventoryDragSource, target);
            return;
        }

        inventoryReturnCoroutine = StartCoroutine(AnimateInventoryReturnCoroutine());
    }

    private bool CanUseInventoryTransferDrag()
    {
        return inventoryTransferEnabled
            && (battleDrag == null || !battleDrag.IsBattleDragEnabled)
            && GameStateManager.CanEditCardCollections();
    }

    private bool HasValidInventoryDragContext()
    {
        return rectTransform != null && canvasRootRect != null && rootCanvas != null;
    }

    private void CacheInventorySlotBeforeDrag()
    {
        inventoryDragWorldPosition = rectTransform.position;
    }

    private void CreateInventoryDragGhost()
    {
        if (inventoryDragGhost != null)
            Destroy(inventoryDragGhost.gameObject);

        GameObject ghost = Instantiate(gameObject, canvasRootRect, worldPositionStays: true);
        ghost.name = $"{name}_DragGhost";
        inventoryDragGhost = ghost.transform as RectTransform;
        if (inventoryDragGhost == null)
            return;

        Vector2 sourceSize = rectTransform.rect.size;
        inventoryDragGhost.anchorMin = new Vector2(0.5f, 0.5f);
        inventoryDragGhost.anchorMax = new Vector2(0.5f, 0.5f);
        inventoryDragGhost.pivot = rectTransform.pivot;
        inventoryDragGhost.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sourceSize.x);
        inventoryDragGhost.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sourceSize.y);
        inventoryDragGhost.position = rectTransform.position;
        inventoryDragGhost.rotation = rectTransform.rotation;
        inventoryDragGhost.SetAsLastSibling();

        foreach (CardView ghostCardView in ghost.GetComponentsInChildren<CardView>(true))
        {
            ghostCardView.SetSelected(false);
            ghostCardView.enabled = false;
        }

        foreach (Graphic ghostGraphic in ghost.GetComponentsInChildren<Graphic>(true))
        {
            Color color = ghostGraphic.color;
            color.a = 1f;
            ghostGraphic.color = color;
            ghostGraphic.raycastTarget = false;
        }

        foreach (Selectable ghostSelectable in ghost.GetComponentsInChildren<Selectable>(true))
        {
            ghostSelectable.transition = Selectable.Transition.None;
            ghostSelectable.enabled = false;
        }

        foreach (CanvasGroup group in ghost.GetComponentsInChildren<CanvasGroup>(true))
        {
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        CanvasGroup ghostCanvasGroup = ghost.GetComponent<CanvasGroup>();
        if (ghostCanvasGroup == null)
            ghostCanvasGroup = ghost.AddComponent<CanvasGroup>();

        ghostCanvasGroup.alpha = 1f;
        ghostCanvasGroup.interactable = false;
        ghostCanvasGroup.blocksRaycasts = false;

        LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryDragGhost);
        Canvas.ForceUpdateCanvases();
    }

    private void CacheInventoryDragPointerOffset(PointerEventData eventData)
    {
        RectTransform dragRect = GetInventoryDragVisualRect();
        if (dragRect == null)
            return;

        if (!TryScreenToInventoryCanvasLocal(eventData.position, out Vector2 pressLocal))
            pressLocal = dragRect.anchoredPosition;

        inventoryDragPointerOffset = dragRect.anchoredPosition - pressLocal;
    }

    private bool TryScreenToInventoryCanvasLocal(Vector2 screenPosition, out Vector2 localPoint)
    {
        localPoint = default;
        if (canvasRootRect == null)
            return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRootRect,
            screenPosition,
            GetInventoryUiEventCamera(),
            out localPoint);
    }

    private Camera GetInventoryUiEventCamera()
    {
        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
    }

    private InventoryCardLocation ResolveInventoryDropTarget(PointerEventData eventData)
    {
        Camera camera = GetInventoryUiEventCamera();

        InventoryCardLocation visualTarget = ResolveInventoryDropTargetFromDraggedCard(camera);
        if (visualTarget != InventoryCardLocation.None)
            return visualTarget;

        InventoryCardLocation raycastTarget = ResolveInventoryDropTargetFromRaycast(eventData);
        if (raycastTarget != InventoryCardLocation.None)
            return raycastTarget;

        if (IsScreenPointInside(deckDropArea, eventData.position, camera))
            return InventoryCardLocation.Deck;

        if (IsScreenPointInside(inventoryDropArea, eventData.position, camera))
            return InventoryCardLocation.Inventory;

        if (TryResolveInventoryDropTargetByColumn(eventData.position, camera, out InventoryCardLocation columnTarget))
            return columnTarget;

        return InventoryCardLocation.None;
    }

    private InventoryCardLocation ResolveInventoryDropTargetFromDraggedCard(Camera camera)
    {
        RectTransform dragRect = GetInventoryDragVisualRect();
        if (!TryGetScreenRect(dragRect, camera, out Rect cardRect))
            return InventoryCardLocation.None;

        bool hasDeckRect = TryGetScreenRect(deckDropArea, camera, out Rect deckRect);
        bool hasInventoryRect = TryGetScreenRect(inventoryDropArea, camera, out Rect inventoryRect);

        if (hasDeckRect && IsXInsideRect(cardRect.center.x, deckRect))
            return InventoryCardLocation.Deck;

        if (hasInventoryRect && IsXInsideRect(cardRect.center.x, inventoryRect))
            return InventoryCardLocation.Inventory;

        bool cardCenterOverDeck = hasDeckRect && deckRect.Contains(cardRect.center);
        if (cardCenterOverDeck)
            return InventoryCardLocation.Deck;

        bool cardCenterOverInventory = hasInventoryRect && inventoryRect.Contains(cardRect.center);
        if (cardCenterOverInventory)
            return InventoryCardLocation.Inventory;

        float deckOverlap = deckRect.width > 0f ? GetOverlapArea(cardRect, deckRect) : 0f;
        float inventoryOverlap = inventoryRect.width > 0f ? GetOverlapArea(cardRect, inventoryRect) : 0f;

        if (deckOverlap <= 0f && inventoryOverlap <= 0f)
            return InventoryCardLocation.None;

        return deckOverlap >= inventoryOverlap
            ? InventoryCardLocation.Deck
            : InventoryCardLocation.Inventory;
    }

    private InventoryCardLocation ResolveInventoryDropTargetFromRaycast(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return InventoryCardLocation.None;

        InventoryDropRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, InventoryDropRaycastResults);

        for (int i = 0; i < InventoryDropRaycastResults.Count; i++)
        {
            Transform hitTransform = InventoryDropRaycastResults[i].gameObject.transform;
            if (IsTransformInsideDropArea(hitTransform, deckDropArea))
                return InventoryCardLocation.Deck;
            if (IsTransformInsideDropArea(hitTransform, inventoryDropArea))
                return InventoryCardLocation.Inventory;
        }

        return InventoryCardLocation.None;
    }

    private static bool IsTransformInsideDropArea(Transform transformToCheck, RectTransform dropArea)
    {
        return transformToCheck != null
            && dropArea != null
            && (transformToCheck == dropArea || transformToCheck.IsChildOf(dropArea));
    }

    private static bool IsScreenPointInside(RectTransform rect, Vector2 screenPosition, Camera camera)
    {
        return rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, camera);
    }

    private static float GetOverlapArea(Rect first, Rect second)
    {
        float width = Mathf.Max(0f, Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin));
        float height = Mathf.Max(0f, Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin));
        return width * height;
    }

    private static bool IsXInsideRect(float x, Rect rect)
    {
        return x >= rect.xMin && x <= rect.xMax;
    }

    private bool TryResolveInventoryDropTargetByColumn(
        Vector2 screenPosition,
        Camera camera,
        out InventoryCardLocation target)
    {
        target = InventoryCardLocation.None;
        if (!TryGetScreenRect(inventoryDropArea, camera, out Rect inventoryRect)
            || !TryGetScreenRect(deckDropArea, camera, out Rect deckRect))
            return false;

        if (IsXInsideRect(screenPosition.x, deckRect))
        {
            target = InventoryCardLocation.Deck;
            return true;
        }

        if (IsXInsideRect(screenPosition.x, inventoryRect))
        {
            target = InventoryCardLocation.Inventory;
            return true;
        }

        float midpoint = (inventoryRect.center.x + deckRect.center.x) * 0.5f;
        target = screenPosition.x >= midpoint
            ? InventoryCardLocation.Deck
            : InventoryCardLocation.Inventory;
        return true;
    }

    private static bool TryGetScreenRect(RectTransform rectTransform, Camera camera, out Rect rect)
    {
        rect = default;
        if (rectTransform == null)
            return false;

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = min;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private RectTransform GetInventoryDragVisualRect()
    {
        return inventoryDragGhost != null ? inventoryDragGhost : rectTransform;
    }

    private void ForceReleaseInventoryTransferDrag(bool suppressClick)
    {
        inventoryTransferDragging = false;
        suppressInventoryClickAfterDrag = suppressClick;

        CancelInventoryReturnAnimationIfRunning();
        RestoreInventoryDragSlot();
    }

    private IEnumerator AnimateInventoryReturnCoroutine()
    {
        try
        {
            RectTransform dragRect = GetInventoryDragVisualRect();
            if (dragRect == null)
                yield break;

            Vector3 from = dragRect.position;
            Vector3 to = inventoryDragWorldPosition;
            float duration = Mathf.Max(1e-4f, returnAnimationDuration);

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
            {
                float eased = EaseOutCubic(Mathf.Clamp01(t));
                dragRect.position = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            dragRect.position = to;
        }
        finally
        {
            RestoreInventoryDragSlot();
            inventoryReturnCoroutine = null;
        }
    }

    private void RestoreInventoryDragSlot()
    {
        if (inventoryDragGhost == null)
            return;

        Destroy(inventoryDragGhost.gameObject);
        inventoryDragGhost = null;
    }

    private void CancelInventoryReturnAnimationIfRunning()
    {
        if (inventoryReturnCoroutine == null)
            return;

        StopCoroutine(inventoryReturnCoroutine);
        inventoryReturnCoroutine = null;
        RestoreInventoryDragSlot();
    }

    private bool ConsumeInventoryDragClickSuppression()
    {
        if (!suppressInventoryClickAfterDrag)
            return false;

        suppressInventoryClickAfterDrag = false;
        return true;
    }

    private static float EaseOutCubic(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }
}
