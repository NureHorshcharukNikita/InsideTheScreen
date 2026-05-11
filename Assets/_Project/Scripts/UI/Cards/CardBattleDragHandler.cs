using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class CardBattleDragHandler
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

        returnCoroutine = owner.StartCoroutine(AnimateReturnToHandCoroutine());
    }

    private bool CanBeginBattleDrag(PointerEventData eventData)
    {
        return eventData.button == PointerEventData.InputButton.Left && HasValidDragContext();
    }

    private void CacheDragPointerOffset(PointerEventData eventData)
    {
        if (!TryScreenToCanvasLocal(eventData.position, out Vector2 pressLocal))
            pressLocal = rectTransform.anchoredPosition;

        dragPointerOffset = rectTransform.anchoredPosition - pressLocal;
    }

}
