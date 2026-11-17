using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// How a dialogue choice looks when the mouse or the keyboard is on it: arrows at both ends, the
// lighter background and text, and the button a little bigger. The text stays where it is, centred
// between room left for the arrows.
[RequireComponent(typeof(Button))]
public sealed class DialogueChoiceButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private static readonly Color NormalText = new(0.94f, 0.88f, 0.80f, 1f);
    private static readonly Color HighlightedText = new(1f, 0.91f, 0.68f, 1f);
    // Room on each side of the text for the arrows (selectionIndicator, rightSelectionIndicator).
    private const float TextSidePadding = 58f;

    // The room the text needs on its sides, so the widest choice still fits.
    public const float HorizontalPadding = TextSidePadding * 2f;

    [SerializeField] private TMP_Text label;
    [SerializeField] private Image background;
    [SerializeField] private Outline outline;
    [SerializeField] private Image selectionIndicator;
    [Tooltip("The arrow at the right end, mirrored, shown with the left one. Empty = the left one alone.")]
    [SerializeField] private Image rightSelectionIndicator;
    [SerializeField] private Sprite normalBackgroundSprite;
    [SerializeField] private Sprite highlightedBackgroundSprite;
    [Tooltip("How much bigger the choice the mouse or keyboard is on grows. 1 = not at all.")]
    [SerializeField, Min(1f)] private float highlightedScale = 1.04f;
    [Tooltip("How long it takes to grow, or to shrink back, in seconds.")]
    [SerializeField, Min(0.01f)] private float scaleSeconds = 0.06f;

    private string normalText = string.Empty;
    private float targetScale = 1f;

    private void Awake()
    {
        ResolveReferences();
        normalText = label != null ? label.text : string.Empty;
        SetHighlighted(false);
    }

    private void OnDisable()
    {
        SetHighlighted(false);
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        float scale = transform.localScale.x;
        if (Mathf.Approximately(scale, targetScale))
            return;

        // Dialogues stop the game clock: on unscaled time.
        float step = Mathf.Max(highlightedScale - 1f, 0.001f) / scaleSeconds * Time.unscaledDeltaTime;
        scale = Mathf.MoveTowards(scale, targetScale, step);
        transform.localScale = new Vector3(scale, scale, 1f);
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
        if (rightSelectionIndicator != null)
            rightSelectionIndicator.gameObject.SetActive(highlighted);

        targetScale = highlighted ? highlightedScale : 1f;
    }

    // The same room on both sides, so centred text stays put whether highlighted or not.
    private void ConfigureLabelLayout()
    {
        if (label == null)
            return;

        RectTransform rect = label.rectTransform;
        rect.offsetMin = new Vector2(TextSidePadding, rect.offsetMin.y);
        rect.offsetMax = new Vector2(-TextSidePadding, rect.offsetMax.y);
    }
}
