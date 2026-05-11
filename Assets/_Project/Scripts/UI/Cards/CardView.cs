using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class CardView : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    private int cardIndex;
    private Action<int> onPointerDown;

    private RectTransform rectTransform;
    private RectTransform canvasRootRect;
    private Canvas rootCanvas;
    private Transform handParent;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;

    private CardBattleDragHandler battleDrag;

    internal int CardIndex => cardIndex;

    private void Awake()
    {
        CacheRectHierarchy();
        EnsureCanvasGroupForRaycasts();
        battleDrag = new CardBattleDragHandler(this);
        battleDrag.SyncHierarchy(rectTransform, handParent, canvasRootRect, rootCanvas, layoutElement, canvasGroup);
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
        cardIndex = index;
        onPointerDown = onSelect;

        battleDrag.Configure(battle, returnAnimationDuration);

        ApplyCardData(data, count);
        SetSelected(selected);
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
        onPointerDown?.Invoke(cardIndex);
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

}
