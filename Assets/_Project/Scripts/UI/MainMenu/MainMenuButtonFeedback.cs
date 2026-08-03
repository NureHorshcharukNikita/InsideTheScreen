using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Selectable selectable;
    [SerializeField] private Graphic background;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalBackground = new Color(0.18f, 0.36f, 0.29f, 0.92f);
    [SerializeField] private Color selectedBackground = new Color(0.30f, 0.76f, 0.54f, 0.96f);
    [SerializeField] private Color disabledBackground = new Color(0.18f, 0.36f, 0.29f, 0.34f);
    [SerializeField] private Color normalText = new Color(0.88f, 1f, 0.92f, 1f);
    [SerializeField] private Color selectedText = Color.white;
    [SerializeField] private Color disabledText = new Color(0.88f, 1f, 0.92f, 0.42f);
    [SerializeField] private float selectedScale = 1.06f;

    private bool isSelected;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;
        if (selectable == null)
            selectable = GetComponent<Selectable>();
        Apply(false);
    }

    private void Start()
    {
        Apply(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable())
            return;

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
            target.localScale = Vector3.one * (selected ? selectedScale : 1f);
        if (background != null)
            background.color = GetBackgroundColor(selected);
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
}
