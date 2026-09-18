using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class QuantitySelectionPrompt : MonoBehaviour
{
    private static readonly Color PanelColor = new(0.31f, 0.23f, 0.2f, 1f);
    private static readonly Color ButtonColor = new(0.66f, 0.59f, 0.55f, 1f);
    private static readonly Color ButtonHighlightedColor = new(0.82f, 0.75f, 0.7f, 1f);
    private static readonly Color ButtonPressedColor = new(0.56f, 0.49f, 0.45f, 1f);
    private static readonly Color ConfirmButtonColor = new(0.48f, 0.37f, 0.32f, 1f);
    private static readonly Color ConfirmButtonHighlightedColor = new(0.58f, 0.46f, 0.4f, 1f);
    private static readonly Color ConfirmButtonPressedColor = new(0.38f, 0.28f, 0.24f, 1f);
    private static readonly Color TextColor = new(0.96f, 0.945f, 0.91f, 1f);

    private TMP_Text titleText;
    private TMP_Text confirmButtonText;
    private Slider slider;
    private TMP_InputField input;
    private Action<int> pendingConfirm;
    private int maxQuantity = 1;
    private bool updatingValue;
    private TMP_FontAsset font;
    private Material fontMaterial;

    public bool IsOpen => gameObject.activeSelf;

    public static QuantitySelectionPrompt Create(Transform parent, TMP_Text referenceText)
    {
        RectTransform overlay = CreateUIObject("QuantitySelectionOverlay", parent);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.pivot = new Vector2(0.5f, 0.5f);
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        overlay.localScale = Vector3.one;

        Image overlayImage = overlay.gameObject.AddComponent<Image>();
        overlayImage.color = new Color(0.04f, 0.03f, 0.025f, 0.5f);
        overlayImage.raycastTarget = true;

        QuantitySelectionPrompt prompt = overlay.gameObject.AddComponent<QuantitySelectionPrompt>();
        prompt.font = referenceText != null ? referenceText.font : null;
        prompt.fontMaterial = referenceText != null ? referenceText.fontMaterial : null;
        prompt.Build();
        prompt.Hide();
        return prompt;
    }

    public void Show(string title, string confirmLabel, int maximum, Action<int> onConfirmed)
    {
        maxQuantity = Mathf.Max(1, maximum);
        pendingConfirm = onConfirmed;
        titleText.text = title;
        confirmButtonText.text = confirmLabel;

        slider.minValue = 1f;
        slider.maxValue = maxQuantity;
        slider.interactable = maxQuantity > 1;
        input.interactable = maxQuantity > 1;

        SetValue(1);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void Hide()
    {
        pendingConfirm = null;
        gameObject.SetActive(false);
    }

    private void Build()
    {
        GameObject panel = CreatePanel("QuantitySelectionPanel", transform, new Vector2(500f, 330f));

        titleText = CreateText(
            "QuantitySelectionTitle",
            panel.transform,
            "Select quantity",
            32,
            new Vector2(0f, 112f),
            new Vector2(420f, 52f),
            TextAlignmentOptions.Center);

        CreateText(
            "QuantitySelectionLabel",
            panel.transform,
            "Quantity",
            25,
            new Vector2(0f, 47f),
            new Vector2(320f, 36f),
            TextAlignmentOptions.Center);

        slider = CreateSlider(panel.transform);
        input = CreateInput(panel.transform);

        Button confirmButton = CreateButton(
            "QuantitySelectionConfirmButton",
            panel.transform,
            "Confirm",
            new Vector2(-115f, -112f),
            new Vector2(180f, 50f),
            Confirm,
            ConfirmButtonColor,
            ConfirmButtonHighlightedColor,
            ConfirmButtonPressedColor);
        confirmButtonText = confirmButton.GetComponentInChildren<TMP_Text>();

        CreateButton(
            "QuantitySelectionCancelButton",
            panel.transform,
            "Cancel",
            new Vector2(115f, -112f),
            new Vector2(180f, 50f),
            Hide,
            ButtonColor,
            ButtonHighlightedColor,
            ButtonPressedColor);
    }

    private Slider CreateSlider(Transform parent)
    {
        RectTransform sliderRect = CreateUIObject("QuantitySelectionSlider", parent);
        SetCenteredRect(sliderRect, new Vector2(350f, 34f), new Vector2(0f, 8f));

        Slider quantitySlider = sliderRect.gameObject.AddComponent<Slider>();
        quantitySlider.wholeNumbers = true;
        quantitySlider.minValue = 1f;
        quantitySlider.maxValue = 1f;
        quantitySlider.value = 1f;

        RectTransform background = CreateUIObject("Background", sliderRect);
        StretchToParent(background, 12f, 11f, 12f, 11f);
        background.gameObject.AddComponent<Image>().color = new Color(0.42f, 0.30f, 0.25f, 1f);

        RectTransform fillArea = CreateUIObject("Fill Area", sliderRect);
        StretchToParent(fillArea, 12f, 11f, 12f, 11f);
        RectTransform fill = CreateUIObject("Fill", fillArea);
        StretchToParent(fill, 0f, 0f, 0f, 0f);
        fill.gameObject.AddComponent<Image>().color = new Color(0.73f, 0.61f, 0.52f, 1f);

        RectTransform handleArea = CreateUIObject("Handle Slide Area", sliderRect);
        StretchToParent(handleArea, 12f, 0f, 12f, 0f);
        RectTransform handle = CreateUIObject("Handle", handleArea);
        handle.sizeDelta = new Vector2(26f, 26f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = TextColor;

        quantitySlider.fillRect = fill;
        quantitySlider.handleRect = handle;
        quantitySlider.targetGraphic = handleImage;
        quantitySlider.direction = Slider.Direction.LeftToRight;
        quantitySlider.onValueChanged.AddListener(OnSliderChanged);
        return quantitySlider;
    }

    private TMP_InputField CreateInput(Transform parent)
    {
        RectTransform inputRect = CreateUIObject("QuantitySelectionInput", parent);
        SetCenteredRect(inputRect, new Vector2(130f, 44f), new Vector2(0f, -45f));
        inputRect.gameObject.AddComponent<Image>().color = new Color(0.21f, 0.15f, 0.13f, 1f);

        TMP_InputField quantityInput = inputRect.gameObject.AddComponent<TMP_InputField>();
        quantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        quantityInput.lineType = TMP_InputField.LineType.SingleLine;
        quantityInput.characterLimit = 6;

        TMP_Text valueText = CreateText("Text", inputRect, "1", 26, Vector2.zero, new Vector2(110f, 34f), TextAlignmentOptions.Center);
        TMP_Text placeholder = CreateText("Placeholder", inputRect, "1", 26, Vector2.zero, new Vector2(110f, 34f), TextAlignmentOptions.Center);
        placeholder.color = new Color(TextColor.r, TextColor.g, TextColor.b, 0.45f);

        quantityInput.textComponent = valueText;
        quantityInput.textViewport = inputRect;
        quantityInput.placeholder = placeholder;
        quantityInput.text = "1";
        quantityInput.onValueChanged.AddListener(OnInputChanged);
        quantityInput.onEndEdit.AddListener(OnInputEndEdit);
        return quantityInput;
    }

    private void Confirm()
    {
        int quantity = GetInputValue();
        Action<int> callback = pendingConfirm;
        Hide();
        callback?.Invoke(quantity);
    }

    private void OnSliderChanged(float value)
    {
        if (!updatingValue)
            SetValue(Mathf.RoundToInt(value));
    }

    private void OnInputChanged(string value)
    {
        if (!updatingValue && int.TryParse(value, out int quantity))
            SetValue(quantity);
    }

    private void OnInputEndEdit(string value)
    {
        SetValue(int.TryParse(value, out int quantity) ? quantity : 1);
    }

    private void SetValue(int value)
    {
        int clampedValue = Mathf.Clamp(value, 1, maxQuantity);
        updatingValue = true;
        slider.SetValueWithoutNotify(clampedValue);
        input.SetTextWithoutNotify(clampedValue.ToString());
        updatingValue = false;
    }

    private int GetInputValue()
    {
        if (!int.TryParse(input.text, out int quantity))
            quantity = 1;

        return Mathf.Clamp(quantity, 1, maxQuantity);
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        RectTransform rect = CreateUIObject(name, parent);
        SetCenteredRect(rect, size, Vector2.zero);
        rect.gameObject.AddComponent<Image>().color = PanelColor;
        return rect.gameObject;
    }

    private Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction onClick,
        Color normalColor,
        Color highlightedColor,
        Color pressedColor)
    {
        RectTransform rect = CreateUIObject(name, parent);
        SetCenteredRect(rect, size, position);
        rect.gameObject.AddComponent<Image>();

        Button button = rect.gameObject.AddComponent<Button>();
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        if (button.targetGraphic is Image image)
            image.color = normalColor;

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.selectedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        CreateText("Text", rect, label, 26, Vector2.zero, size, TextAlignmentOptions.Center);
        return button;
    }

    private TMP_Text CreateText(string name, Transform parent, string value, float size, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateUIObject(name, parent);
        SetCenteredRect(rect, dimensions, position);

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            label.font = font;
            label.fontMaterial = fontMaterial;
        }

        label.text = value;
        label.fontSize = size;
        label.color = TextColor;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void StretchToParent(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }
}
