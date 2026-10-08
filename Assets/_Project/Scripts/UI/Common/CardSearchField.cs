using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Turns the search bar art (a SearchField with SearchIcon and Placeholder children)
// into a working text input, built the same way as the inventory search.
[DisallowMultipleComponent]
public sealed class CardSearchField : MonoBehaviour
{
    private static readonly Color InputTextColor = new(0.96f, 0.95f, 0.92f, 1f);
    private static readonly Color PlaceholderColor = new(0.68f, 0.68f, 0.68f, 1f);
    private const string PlaceholderText = "Search cards...";

    private TMP_InputField input;
    private TMP_Text placeholder;

    public event Action<string> QueryChanged;

    public string Query => input != null ? input.text.Trim() : string.Empty;

    private void Awake()
    {
        EnsureInput();
    }

    private void OnDestroy()
    {
        if (input == null)
            return;

        input.onValueChanged.RemoveListener(HandleValueChanged);
        input.onSelect.RemoveListener(HandleSelected);
        input.onDeselect.RemoveListener(HandleDeselected);
    }

    public void Clear()
    {
        EnsureInput();
        if (input != null)
            input.SetTextWithoutNotify(string.Empty);

        UpdatePlaceholder();
    }

    // Same rule as the inventory search: a case-insensitive part of the card name.
    public static bool Matches(string cardName, string query)
    {
        return string.IsNullOrEmpty(query)
            || (cardName != null && cardName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void EnsureInput()
    {
        if (input != null)
            return;

        RectTransform field = transform as RectTransform;
        Image background = GetComponent<Image>();
        if (field == null || background == null)
            return;

        RectTransform textViewport = field.Find("Text Area") as RectTransform;
        if (textViewport == null)
            textViewport = CreateUIObject("Text Area", field);

        textViewport.anchorMin = Vector2.zero;
        textViewport.anchorMax = Vector2.one;
        textViewport.offsetMin = new Vector2(80f, 8f);
        textViewport.offsetMax = new Vector2(-18f, -8f);
        if (textViewport.GetComponent<RectMask2D>() == null)
            textViewport.gameObject.AddComponent<RectMask2D>();

        placeholder = FindText(textViewport, "Placeholder");
        if (placeholder == null)
            placeholder = FindText(field, "Placeholder");
        if (placeholder == null)
        {
            placeholder = CreateText("Placeholder", textViewport, PlaceholderText, PlaceholderColor, null);
        }
        else
        {
            RectTransform placeholderRect = placeholder.rectTransform;
            placeholderRect.SetParent(textViewport, false);
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
        }

        TMP_Text inputText = FindText(textViewport, "Text");
        if (inputText == null)
            inputText = CreateText("Text", textViewport, string.Empty, InputTextColor, placeholder);

        input = GetComponent<TMP_InputField>();
        if (input == null)
            input = gameObject.AddComponent<TMP_InputField>();

        // TMP creates its caret in OnEnable, after the text references are assigned.
        input.enabled = false;
        input.interactable = true;
        input.readOnly = false;
        input.targetGraphic = background;
        input.textViewport = textViewport;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.characterLimit = 64;
        input.customCaretColor = true;
        input.caretColor = InputTextColor;
        input.caretWidth = 3;
        input.caretBlinkRate = 0.65f;
        input.selectionColor = new Color(0.76f, 0.55f, 0.18f, 0.45f);
        input.onValueChanged.AddListener(HandleValueChanged);
        input.onSelect.AddListener(HandleSelected);
        input.onDeselect.AddListener(HandleDeselected);
        input.enabled = true;
        UpdatePlaceholder();
    }

    private void HandleValueChanged(string _)
    {
        UpdatePlaceholder();
        QueryChanged?.Invoke(Query);
    }

    private void HandleSelected(string _)
    {
        UpdatePlaceholder(true);
    }

    private void HandleDeselected(string _)
    {
        UpdatePlaceholder();
    }

    private void UpdatePlaceholder(bool? focusOverride = null)
    {
        if (placeholder == null)
            return;

        bool hasFocus = focusOverride ?? (input != null && input.isFocused);
        bool showPlaceholder = !hasFocus && (input == null || string.IsNullOrEmpty(input.text));
        placeholder.gameObject.SetActive(showPlaceholder);
    }

    private static TMP_Text FindText(Transform parent, string childName)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        return child != null && child.TryGetComponent(out TMP_Text text) ? text : null;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, string value, Color color, TMP_Text fontSource)
    {
        RectTransform textRect = CreateUIObject(objectName, parent);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        if (fontSource != null)
        {
            text.font = fontSource.font;
            text.fontSharedMaterial = fontSource.fontSharedMaterial;
        }

        text.text = value;
        text.fontSize = 27f;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateUIObject(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }
}
