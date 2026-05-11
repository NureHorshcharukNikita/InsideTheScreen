using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    public void OnPointerDown(PointerEventData eventData)
    {
        onPointerDown?.Invoke(cardIndex);
    }

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

    private void ApplyCardData(CardData data, int? count)
    {
        if (cardNameText != null)
            cardNameText.text = data.CardName;

        if (cardCostText != null)
            cardCostText.text = data.Cost.ToString();

        if (cardDescriptionText != null)
            cardDescriptionText.text = data.Description;

        if (countText != null)
        {
            if (count.HasValue)
            {
                countText.text = $"x{count.Value}";
                countText.gameObject.SetActive(true);
            }
            else
            {
                countText.gameObject.SetActive(false);
            }
        }

        if (cardIcon != null)
        {
            cardIcon.sprite = data.Icon;
            cardIcon.enabled = data.Icon != null;
            cardIcon.preserveAspect = true;
        }
    }
}
