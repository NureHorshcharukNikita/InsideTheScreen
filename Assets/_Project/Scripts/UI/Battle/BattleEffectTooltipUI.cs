using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleEffectTooltipUI : MonoBehaviour
{
    private const float Width = 320f;
    private const float MinHeight = 92f;
    private const float Padding = 12f;
    private const float CursorOffset = 18f;

    private static BattleEffectTooltipUI instance;

    private RectTransform rectTransform;
    private Canvas canvas;
    private TMP_Text titleText;
    private TMP_Text bodyText;

    private void Awake()
    {
        instance = this;
        rectTransform = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        SetPosition(Input.mousePosition);
    }

    public static void Show(string title, string body, Vector2 screenPosition)
    {
        BattleEffectTooltipUI tooltip = EnsureInstance();
        tooltip.gameObject.SetActive(true);
        tooltip.titleText.text = title;
        tooltip.bodyText.text = body;
        tooltip.UpdateSize();
        tooltip.SetPosition(screenPosition);
    }

    public static void Hide()
    {
        if (instance != null)
            instance.gameObject.SetActive(false);
    }

    private static BattleEffectTooltipUI EnsureInstance()
    {
        if (instance != null)
            return instance;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        GameObject tooltipObject = new("BattleEffectTooltip", typeof(RectTransform));
        if (canvas != null)
            tooltipObject.transform.SetParent(canvas.transform, false);

        BattleEffectTooltipUI tooltip = tooltipObject.AddComponent<BattleEffectTooltipUI>();
        tooltip.Build();
        tooltip.gameObject.SetActive(false);
        return tooltip;
    }

    private void Build()
    {
        rectTransform = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        rectTransform.sizeDelta = new Vector2(Width, MinHeight);
        rectTransform.pivot = new Vector2(0f, 1f);

        Image background = gameObject.AddComponent<Image>();
        background.color = new Color(0.16f, 0.1f, 0.08f, 0.96f);
        background.raycastTarget = false;

        VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 5f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        titleText = CreateText("Title", 18f, FontStyles.Bold, 24f);
        bodyText = CreateText("Body", 14f, FontStyles.Normal, 48f);
    }

    private TMP_Text CreateText(string name, float fontSize, FontStyles style, float minHeight)
    {
        GameObject textObject = new(name, typeof(RectTransform));
        textObject.transform.SetParent(transform, false);

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = minHeight;
        layoutElement.flexibleWidth = 1f;

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = new Color(0.96f, 0.91f, 0.86f, 1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void UpdateSize()
    {
        float contentWidth = Width - Padding * 2f;
        float titleHeight = titleText.GetPreferredValues(titleText.text, contentWidth, 0f).y;
        float bodyHeight = bodyText.GetPreferredValues(bodyText.text, contentWidth, 0f).y;
        float height = Mathf.Max(MinHeight, titleHeight + bodyHeight + Padding * 2f + 5f);
        rectTransform.sizeDelta = new Vector2(Width, height);
    }

    private void SetPosition(Vector2 screenPosition)
    {
        Vector2 targetPosition = screenPosition + new Vector2(CursorOffset, -CursorOffset);
        if (canvas == null)
        {
            rectTransform.position = targetPosition;
            return;
        }

        RectTransform canvasRect = (RectTransform)canvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint);

        Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
        Vector2 size = rectTransform.rect.size;
        localPoint.x = Mathf.Clamp(localPoint.x, -halfCanvas.x + Padding, halfCanvas.x - size.x - Padding);
        localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvas.y + size.y + Padding, halfCanvas.y - Padding);
        rectTransform.anchoredPosition = localPoint;
    }
}
