using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Selectable selectable;
    [SerializeField] private Graphic background;
    // Optional: an Image background switches to the lighted panel while the button is selected.
    [SerializeField] private Sprite normalBackgroundSprite;
    [SerializeField] private Sprite selectedBackgroundSprite;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite normalIcon;
    [SerializeField] private Sprite selectedIcon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalBackground = new Color(0.18f, 0.36f, 0.29f, 0.92f);
    [SerializeField] private Color selectedBackground = new Color(0.30f, 0.76f, 0.54f, 0.96f);
    [SerializeField] private Color disabledBackground = new Color(0.18f, 0.36f, 0.29f, 0.34f);
    [SerializeField] private Color normalText = new Color(0.88f, 1f, 0.92f, 1f);
    [SerializeField] private Color selectedText = Color.white;
    [SerializeField] private Color disabledText = new Color(0.88f, 1f, 0.92f, 0.42f);
    [SerializeField] private float selectedScale = 1.06f;

    private bool isSelected;
    private Vector2 normalSize;
    private bool hasNormalSize;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;
        if (selectable == null)
            selectable = GetComponent<Selectable>();
        CacheNormalSize();
        Apply(false);
    }

    private void Start()
    {
        Apply(false);
    }

    private void OnDisable()
    {
        isSelected = false;
        Apply(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

        // Hovering selects the button, so the previously selected one is deselected and
        // only one button is lit at a time for mouse and keyboard alike.
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject != gameObject && !eventSystem.alreadySelecting)
        {
            eventSystem.SetSelectedGameObject(gameObject);
            return;
        }

        Apply(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Apply(isSelected);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!IsInteractable())
            return;

        isSelected = true;
        Apply(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        Apply(false);
    }

    private void Apply(bool selected)
    {
        if (!IsInteractable())
            selected = false;

        if (target != null)
        {
            CacheNormalSize();
            target.localScale = Vector3.one;
            target.sizeDelta = normalSize * (selected ? selectedScale : 1f);
        }
        if (background != null)
        {
            background.color = GetBackgroundColor(selected);
            if (background is Image backgroundImage && selectedBackgroundSprite != null)
                backgroundImage.sprite = selected ? selectedBackgroundSprite : normalBackgroundSprite;
        }
        if (icon != null)
        {
            Sprite iconSprite = selected ? selectedIcon : normalIcon;
            if (iconSprite != null)
                icon.sprite = iconSprite;
            icon.color = GetTextColor(selected);
        }
        if (label != null)
            label.color = GetTextColor(selected);
    }

    private Color GetBackgroundColor(bool selected)
    {
        if (!IsInteractable())
            return disabledBackground;

        return selected ? selectedBackground : normalBackground;
    }

    private Color GetTextColor(bool selected)
    {
        if (!IsInteractable())
            return disabledText;

        return selected ? selectedText : normalText;
    }

    private bool IsInteractable()
    {
        return selectable == null || selectable.interactable;
    }

    private void CacheNormalSize()
    {
        if (target == null || hasNormalSize)
            return;

        normalSize = target.sizeDelta;
        hasNormalSize = true;
    }
}
