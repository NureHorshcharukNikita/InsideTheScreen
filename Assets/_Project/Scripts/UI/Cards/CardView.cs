using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class CardView : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Texts & icon")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text cardCostText;
    [SerializeField] private TMP_Text cardDescriptionText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image cardIcon;
    [SerializeField] private GameObject selectionBorder;

    [Header("Drag")]
    [SerializeField] private float returnAnimationDuration = 0.15f;

    private CardData cardData;
    private int cardIndex;
    private Action<int> onPointerDown;
    private Action<CardData, InventoryCardLocation, InventoryCardLocation> onInventoryDrop;
    private InventoryCardLocation inventoryDragSource;
    private RectTransform inventoryDropArea;
    private RectTransform deckDropArea;
    private bool inventoryTransferEnabled;

    private RectTransform rectTransform;
    private RectTransform canvasRootRect;
    private Canvas rootCanvas;
    private Transform handParent;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;

    private CardBattleDragHandler battleDrag;
    private BattleSystem battleSystem;

    internal int CardIndex => cardIndex;

    private void Awake()
    {
        CacheRectHierarchy();
        EnsureCanvasGroupForRaycasts();
        battleDrag = new CardBattleDragHandler(this);
        battleDrag.SyncHierarchy(rectTransform, handParent, canvasRootRect, rootCanvas, layoutElement, canvasGroup);
    }

    private void Update()
    {
        RefreshBattleInteractionLock();
    }

    public void Setup(CardData data, int index, Action<int> onSelect, BattleSystem battle = null, bool selected = false)
    {
        Setup(data, null, index, onSelect, battle, selected);
    }

    public void Setup(
        CardData data,
        int? count,
        int index,
        Action<int> onSelect,
        BattleSystem battle = null,
        bool selected = false)
    {
        EnsureRuntimeReferences();

        cardIndex = index;
        onPointerDown = onSelect;
        battleSystem = battle;
        cardData = data;
        ClearInventoryTransfer();

        battleDrag?.Configure(battle, returnAnimationDuration);

        ApplyCardData(data, count);
        SetSelected(selected);
    }

    public void ConfigureInventoryTransfer(
        CardData data,
        InventoryCardLocation source,
        RectTransform inventoryArea,
        RectTransform deckArea,
        Action<CardData, InventoryCardLocation, InventoryCardLocation> onDrop)
    {
        cardData = data;
        inventoryDragSource = source;
        inventoryDropArea = inventoryArea;
        deckDropArea = deckArea;
        onInventoryDrop = onDrop;
        inventoryTransferEnabled = data != null
            && source != InventoryCardLocation.None
            && onDrop != null;
    }

    public void ClearInventoryTransfer()
    {
        if (inventoryTransferDragging)
            RestoreInventoryDragSlot();
        CancelInventoryReturnAnimationIfRunning();

        inventoryTransferEnabled = false;
        inventoryDragSource = InventoryCardLocation.None;
        inventoryDropArea = null;
        deckDropArea = null;
        onInventoryDrop = null;
    }

    public void SetSelected(bool selected)
    {
        if (selectionBorder != null)
            selectionBorder.SetActive(selected);
    }

    public void RefreshDragHierarchyAfterReparent()
    {
        CacheRectHierarchy();
        battleDrag.SyncHierarchy(rectTransform, handParent, canvasRootRect, rootCanvas, layoutElement, canvasGroup);
    }

    public void ForceReleaseBattleDragToHand()
    {
        battleDrag?.ForceReleaseDragToHand();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsBattleInteractionLocked())
            return;

        if (CanUseInventoryTransferDrag())
            return;

        onPointerDown?.Invoke(cardIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsBattleInteractionLocked())
            return;

        if (!CanUseInventoryTransferDrag())
            return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (ConsumeInventoryDragClickSuppression())
            return;

        onPointerDown?.Invoke(cardIndex);
    }

    private bool IsBattleInteractionLocked()
    {
        return battleSystem != null && !battleSystem.CanPlay();
    }

    private void RefreshBattleInteractionLock()
    {
        if (battleSystem == null || canvasGroup == null)
            return;

        bool canInteract = battleSystem.CanPlay();
        if (!canInteract)
        {
            SetSelected(false);
            if (battleDrag != null && battleDrag.IsDragging)
                battleDrag.ForceReleaseDragToHand();
        }

        if (battleDrag == null || !battleDrag.IsDragging)
            canvasGroup.blocksRaycasts = canInteract;
    }

    private void CacheRectHierarchy()
    {
        rectTransform = transform as RectTransform;
        handParent = rectTransform != null ? rectTransform.parent : null;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            rootCanvas = canvas.rootCanvas;
            canvasRootRect = rootCanvas.transform as RectTransform;
        }

        layoutElement = GetComponent<LayoutElement>();
    }

    private void EnsureCanvasGroupForRaycasts()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = true;
    }

    private void EnsureRuntimeReferences()
    {
        if (rectTransform == null || rootCanvas == null || layoutElement == null)
            CacheRectHierarchy();

        if (canvasGroup == null)
            EnsureCanvasGroupForRaycasts();

        if (battleDrag == null)
            battleDrag = new CardBattleDragHandler(this);

        battleDrag.SyncHierarchy(rectTransform, handParent, canvasRootRect, rootCanvas, layoutElement, canvasGroup);
    }
}
