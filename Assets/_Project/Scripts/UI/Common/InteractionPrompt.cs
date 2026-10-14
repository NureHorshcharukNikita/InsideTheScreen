using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The prompt over something the player can use: the key on its key cap, then "- Open" on a dark
// rounded panel that starts behind the key (NPCs, doorways, signs). A message on its own, e.g. an
// NPC's overhead line or a sign's text, sits on the panel alone. Built at runtime around the
// prompt's text, inside its world-space canvas; the look is InteractionPromptStyle.
// Shown, the key pops up first and the panel slides out of it to the right; hidden, the panel
// slides back into the key and then the key goes. It turns its canvas on and off itself, so the
// way out can play. On the GameClock: it also finishes as a dialogue starts and pauses the game.
[DisallowMultipleComponent]
public sealed class InteractionPrompt : MonoBehaviour
{
    // Share of the way in (or out) taken by the key; the panel takes the rest.
    private const float KeyShare = 0.4f;
    private const float KeyStartScale = 0.5f;
    private const float MessageStartScale = 0.85f;

    private InteractionPromptStyle style;
    private TMP_Text text;
    private RectTransform body;
    private CanvasGroup bodyGroup;
    private RectTransform slide;
    private RectTransform content;
    private RectTransform panel;
    private Image panelImage;
    private RectTransform key;
    private CanvasGroup keyGroup;
    private Image keyImage;
    private TMP_Text keyLabel;
    private bool showKey;
    private bool layoutPending;
    private bool shown;
    // 0 = hidden, 1 = fully shown; the key over the first part, the panel over the rest.
    private float reveal;

    // Where it is heading: still true while it slides in, already false while it slides out.
    public bool IsShown => shown;

    // The prompt around this text, made the first time. Null without an InteractionPromptStyle in
    // Resources: the text then stays as it is.
    public static InteractionPrompt For(TMP_Text promptText)
    {
        if (promptText == null)
            return null;

        InteractionPrompt prompt = promptText.GetComponentInParent<InteractionPrompt>(true);
        if (prompt != null)
            return prompt;

        InteractionPromptStyle promptStyle = InteractionPromptStyle.Instance;
        Transform canvas = promptText.transform.parent;
        if (promptStyle == null || canvas == null)
            return null;

        prompt = canvas.gameObject.AddComponent<InteractionPrompt>();
        prompt.Build(promptText, promptStyle);
        return prompt;
    }

    public void ShowAction(string keyName, string action)
    {
        showKey = true;
        key.gameObject.SetActive(true);
        keyLabel.text = keyName;
        text.text = string.Format(style.actionFormat, action);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        RequestLayout();
    }

    public void ShowMessage(string message)
    {
        showKey = false;
        key.gameObject.SetActive(false);
        text.text = message;
        text.alignment = TextAlignmentOptions.Midline;
        text.textWrappingMode = TextWrappingModes.Normal;
        RequestLayout();
    }

    public void SetShown(bool value)
    {
        if (value == shown)
        {
            if (!shown && reveal <= 0f && gameObject.activeSelf)
                gameObject.SetActive(false);
            return;
        }

        shown = value;
        if (shown)
        {
            if (!gameObject.activeSelf)
            {
                reveal = 0f;
                gameObject.SetActive(true);
            }

            if (TryGetComponent(out CanvasGroup canvasGroup))
                canvasGroup.alpha = 1f;

            if (layoutPending && isActiveAndEnabled)
                LayoutNow();
            ApplyReveal();
        }
        else if (!gameObject.activeInHierarchy)
        {
            // Nothing on screen to play the way out on.
            reveal = 0f;
            gameObject.SetActive(false);
        }
    }

    // Corners of what is drawn, in world space, e.g. to stand it on top of a sign.
    public Bounds GetWorldBounds()
    {
        LayoutNow();
        var corners = new Vector3[4];
        body.GetWorldCorners(corners);
        var bounds = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
            bounds.Encapsulate(corners[i]);

        return bounds;
    }

    // Laid out where it is drawn: text sizes are only known once the prompt is active.
    private void RequestLayout()
    {
        layoutPending = true;
        if (isActiveAndEnabled)
        {
            LayoutNow();
            ApplyReveal();
        }
    }

    private void LateUpdate()
    {
        bool changed = layoutPending;
        if (layoutPending)
            LayoutNow();

        float target = shown ? 1f : 0f;
        if (!Mathf.Approximately(reveal, target))
        {
            float duration = shown ? style.showDuration : style.hideDuration;
            reveal = Mathf.MoveTowards(reveal, target, duration > 0f ? GameClock.DeltaTime / duration : 1f);
            changed = true;
        }

        if (changed)
            ApplyReveal();

        if (!shown && reveal <= 0f)
            gameObject.SetActive(false);
    }

    private void ApplyReveal()
    {
        if (showKey)
        {
            float keyProgress = Mathf.Clamp01(reveal / KeyShare);
            float panelProgress = Mathf.Clamp01((reveal - KeyShare) / (1f - KeyShare));
            key.localScale = Vector3.one * Mathf.LerpUnclamped(KeyStartScale, 1f, EaseOutBack(keyProgress));
            keyGroup.alpha = Mathf.Clamp01(keyProgress * 2f);
            content.anchoredPosition = new Vector2(-slide.sizeDelta.x * (1f - EaseOutCubic(panelProgress)), 0f);
            body.localScale = Vector3.one;
            bodyGroup.alpha = 1f;
        }
        else
        {
            content.anchoredPosition = Vector2.zero;
            body.localScale = Vector3.one * Mathf.LerpUnclamped(MessageStartScale, 1f, EaseOutBack(reveal));
            bodyGroup.alpha = reveal;
        }
    }

    private void LayoutNow()
    {
        layoutPending = false;
        if (showKey)
            LayoutAction();
        else
            LayoutMessage();

        FitSliced(panelImage, style.panelHeight);
        FitSliced(keyImage, style.keyHeight);
    }

    // [E] - Open: the panel starts at the middle of the key, the text right after the key. They
    // slide out from under the key, clipped at its middle.
    private void LayoutAction()
    {
        Sprite keySprite = keyImage.sprite;
        float keyWidth = keySprite != null ? style.keyHeight * keySprite.rect.width / keySprite.rect.height : style.keyHeight;
        float labelWidth = keyLabel.GetPreferredValues(keyLabel.text, float.PositiveInfinity, float.PositiveInfinity).x;
        keyWidth = Mathf.Max(keyWidth, labelWidth + 2f * style.keyTextPadding);

        float textWidth = text.GetPreferredValues(text.text, float.PositiveInfinity, float.PositiveInfinity).x;
        float panelStart = keyWidth * 0.5f;
        float textStart = keyWidth + style.textGap;
        float width = textStart + textWidth + style.panelPadding;
        float height = Mathf.Max(style.keyHeight, style.panelHeight);
        float panelWidth = width - panelStart;

        body.sizeDelta = new Vector2(width, height);
        Place(slide, panelStart, panelWidth, height);
        Place(content, 0f, panelWidth, height);
        Place(panel, 0f, panelWidth, style.panelHeight);
        Place(text.rectTransform, textStart - panelStart, textWidth + 1f, style.panelHeight);

        key.anchorMin = key.anchorMax = new Vector2(0f, 0.5f);
        key.pivot = new Vector2(0.5f, 0.5f);
        key.anchoredPosition = new Vector2(keyWidth * 0.5f, 0f);
        key.sizeDelta = new Vector2(keyWidth, style.keyHeight);
    }

    // A message on the panel alone, wrapped to the width of the prompt's canvas.
    private void LayoutMessage()
    {
        float canvasWidth = ((RectTransform)transform).rect.width;
        float maxWidth = Mathf.Max(1f, canvasWidth - 2f * style.panelPadding);
        Vector2 size = text.GetPreferredValues(text.text, maxWidth, float.PositiveInfinity);
        float textWidth = Mathf.Min(size.x, maxWidth);
        float height = Mathf.Max(style.panelHeight, size.y + 2f * style.messagePadding);
        float width = textWidth + 2f * style.panelPadding;

        body.sizeDelta = new Vector2(width, height);
        Place(slide, 0f, width, height);
        Place(content, 0f, width, height);
        Place(panel, 0f, width, height);
        Place(text.rectTransform, style.panelPadding, textWidth + 1f, height);
    }

    private void Build(TMP_Text promptText, InteractionPromptStyle promptStyle)
    {
        // Starts hidden, without playing the way out: the first SetShown(true) brings it in.
        style = promptStyle;
        text = promptText;
        shown = false;
        reveal = 0f;

        body = CreateRect("Prompt", transform);
        body.anchorMin = body.anchorMax = new Vector2(0.5f, 0.5f);
        body.pivot = new Vector2(0.5f, 0.5f);
        body.anchoredPosition = Vector2.zero;
        bodyGroup = body.gameObject.AddComponent<CanvasGroup>();

        // Drawn in this order: the panel, the text over it, then the key over the panel's end.
        slide = CreateRect("Slide", body);
        slide.gameObject.AddComponent<RectMask2D>();
        content = CreateRect("Content", slide);

        panel = CreateRect("Panel", content);
        panelImage = AddImage(panel, style.panelSprite, style.panelColor);

        text.rectTransform.SetParent(content, false);
        text.fontSize = style.fontSize;
        text.color = style.textColor;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;

        key = CreateRect("Key", body);
        keyGroup = key.gameObject.AddComponent<CanvasGroup>();
        keyImage = AddImage(key, style.keySprite, Color.white);

        RectTransform labelRect = CreateRect("KeyLabel", key);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        keyLabel = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        keyLabel.font = text.font;
        keyLabel.fontSize = style.keyFontSize;
        keyLabel.fontStyle = FontStyles.Bold;
        keyLabel.color = style.keyTextColor;
        keyLabel.alignment = TextAlignmentOptions.Center;
        keyLabel.textWrappingMode = TextWrappingModes.NoWrap;
        keyLabel.raycastTarget = false;
    }

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        var child = new GameObject(objectName, typeof(RectTransform));
        child.layer = gameObject.layer;
        var rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image AddImage(RectTransform rect, Sprite sprite, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Left edge `left` units into the parent, centred vertically.
    private static void Place(RectTransform rect, float left, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(left, 0f);
        rect.sizeDelta = new Vector2(width, height);
    }

    // Borders scaled so the whole sprite height fits `height`: the panel's ends stay round and the
    // key cap's frame keeps its thickness, however wide they stretch.
    private static void FitSliced(Image image, float height)
    {
        Sprite sprite = image.sprite;
        if (sprite == null || height <= 0f)
            return;

        Canvas canvas = image.canvas;
        float referencePixelsPerUnit = canvas != null ? canvas.referencePixelsPerUnit : 100f;
        image.pixelsPerUnitMultiplier = sprite.rect.height * referencePixelsPerUnit / (sprite.pixelsPerUnit * height);
    }

    // A little past full size, then back: the key pops up.
    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        float u = t - 1f;
        return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
    }

    private static float EaseOutCubic(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }
}
