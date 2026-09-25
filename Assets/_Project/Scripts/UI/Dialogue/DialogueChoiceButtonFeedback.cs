using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class DialogueChoiceButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private static readonly Color NormalText = new(0.94f, 0.88f, 0.80f, 1f);
    private static readonly Color HighlightedText = new(1f, 0.91f, 0.68f, 1f);
    private const float ChoiceTextLeftPadding = 58f;

    [SerializeField] private TMP_Text label;
    [SerializeField] private Image background;
    [SerializeField] private Outline outline;
    [SerializeField] private Image selectionIndicator;
    [SerializeField] private Sprite normalBackgroundSprite;
    [SerializeField] private Sprite highlightedBackgroundSprite;

    private string normalText = string.Empty;

    private void Awake()
    {
        ResolveReferences();
        normalText = label != null ? label.text : string.Empty;
        SetHighlighted(false);
    }

    private void OnDisable()
    {
        SetHighlighted(false);
    }

    public void Configure(TMP_Text textLabel, string text)
    {
        ResolveReferences();
        if (textLabel != null)
            label = textLabel;

        normalText = text ?? string.Empty;
        ConfigureLabelLayout();
        SetHighlighted(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlighted(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHighlighted(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetHighlighted(false);
    }

    private void ResolveReferences()
    {
        if (label == null)
            label = GetComponentInChildren<TMP_Text>(true);
        if (background == null)
            background = GetComponent<Image>();
        if (outline == null)
            outline = GetComponent<Outline>();
    }

    private void SetHighlighted(bool highlighted)
    {
        if (label != null)
        {
            label.text = normalText;
            label.color = highlighted ? HighlightedText : NormalText;
        }

        if (background != null)
        {
            Sprite sprite = highlighted ? highlightedBackgroundSprite : normalBackgroundSprite;
            if (sprite != null)
                background.sprite = sprite;
            background.color = Color.white;
        }

        if (outline != null)
            outline.enabled = false;

        if (selectionIndicator != null)
            selectionIndicator.gameObject.SetActive(highlighted);
    }

    private void ConfigureLabelLayout()
    {
        if (label == null)
            return;

        RectTransform rect = label.rectTransform;
        Vector2 offsetMin = rect.offsetMin;
        Vector2 offsetMax = rect.offsetMax;
        offsetMin.x = ChoiceTextLeftPadding;
        offsetMax.x = -24f;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

}
