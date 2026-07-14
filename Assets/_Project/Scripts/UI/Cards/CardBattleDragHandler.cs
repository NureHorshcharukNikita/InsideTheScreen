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
    public bool IsDragging { get; private set; }

    public void Configure(BattleSystem battle, float returnDuration)
    {
        battleSystem = battle;
        returnAnimationDuration = returnDuration;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsBattleDragEnabled)
            return;

        if (!battleSystem.CanPlay())
            return;

        if (!CanBeginBattleDrag(eventData))
            return;

        CancelReturnAnimationIfRunning();
        CacheHandSlotBeforeDrag();
        IsDragging = true;

        MoveUnderRootCanvasUnclipped();
        SetLayoutIgnoredForDrag(true);

        CacheDragPointerOffset(eventData);
        SetRaycastBlocking(false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsBattleDragEnabled)
            return;

        if (!battleSystem.CanPlay())
        {
            ForceReleaseDragToHand();
            return;
        }

        if (!HasValidDragContext())
            return;

        if (!TryScreenToCanvasLocal(eventData.position, out Vector2 localPoint))
            return;

        rectTransform.anchoredPosition = localPoint + dragPointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsBattleDragEnabled)
            return;

        if (!battleSystem.CanPlay())
        {
            IsDragging = false;
            if (HasValidDragContext())
                returnCoroutine = owner.StartCoroutine(AnimateReturnToHandCoroutine());
            else
                SetRaycastBlocking(true);

            return;
        }

        if (!HasValidDragContext())
        {
            IsDragging = false;
            SetRaycastBlocking(true);
            return;
        }

        if (TryPlayOnDropTarget(eventData.position))
        {
            IsDragging = false;
            SetLayoutIgnoredForDrag(false);
            SetRaycastBlocking(true);
            Object.Destroy(owner.gameObject);
            return;
        }

        IsDragging = false;
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
