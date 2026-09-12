using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class DialogueChoiceButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private static readonly Color NormalBackground = new(0.08f, 0.045f, 0.04f, 0.96f);
    private static readonly Color HighlightedBackground = new(0.19f, 0.115f, 0.075f, 1f);
    private static readonly Color NormalOutline = new(0.73f, 0.56f, 0.42f, 0.9f);
    private static readonly Color HighlightedOutline = new(0.94f, 0.68f, 0.34f, 1f);
    private static readonly Color NormalText = new(0.94f, 0.88f, 0.80f, 1f);
    private static readonly Color HighlightedText = new(1f, 0.91f, 0.68f, 1f);

    [SerializeField] private TMP_Text label;
    [SerializeField] private Image background;
    [SerializeField] private Outline outline;

    private string normalText = string.Empty;
    private string hoverPrefix = "> ";

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

    public void Configure(TMP_Text textLabel, string text, string prefix)
    {
        ResolveReferences();
        if (textLabel != null)
            label = textLabel;

        normalText = text ?? string.Empty;
        hoverPrefix = string.IsNullOrEmpty(prefix) ? "> " : prefix;
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
            label.text = highlighted ? hoverPrefix + normalText : normalText;
            label.color = highlighted ? HighlightedText : NormalText;
        }

        if (background != null)
            background.color = highlighted ? HighlightedBackground : NormalBackground;

        if (outline != null)
            outline.effectColor = highlighted ? HighlightedOutline : NormalOutline;
    }
}
