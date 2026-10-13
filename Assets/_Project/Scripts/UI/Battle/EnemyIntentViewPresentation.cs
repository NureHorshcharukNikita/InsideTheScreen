using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class EnemyIntentPresenter
{
    private const string TargetIconName = "TargetIcon";
    private const float TargetIconGap = 8f;
    // The target icon's size, as a share of the label's font size.
    private const float TargetIconScale = 2f;

    private readonly GameObject owner;
    private readonly TMP_Text intentLabel;
    private readonly Image intentIcon;
    private readonly GameObject intentContainer;
    private readonly Vector4 labelBaseMargin;

    private Image targetIcon;
    private Color labelBaseColor = Color.white;
    private Color iconBaseColor = Color.white;
    private bool capturedBaseColors;

    public EnemyIntentPresenter(GameObject owner, TMP_Text intentLabel, Image intentIcon, GameObject intentContainer)
    {
        this.owner = owner;
        this.intentLabel = intentLabel;
        this.intentIcon = intentIcon;
        this.intentContainer = intentContainer;
        labelBaseMargin = intentLabel != null ? intentLabel.margin : Vector4.zero;
    }

    public bool HasLabel => intentLabel != null;

    public void ShowEmpty()
    {
        if (intentLabel != null)
        {
            intentLabel.text = "";
            intentLabel.enabled = false;
        }

        SetContainerActive(false);
        ClearIcon();
        SetTargetIcon(null, false);
        SetVisualAlpha(1f);
    }

    // targetSprite: the battle icon of the one it will hit, shown after the text; null = none.
    public void ShowIntent(string text, Sprite icon, Sprite targetSprite, bool keepVisualHidden)
    {
        CaptureBaseColorsIfNeeded();

        bool show = !string.IsNullOrEmpty(text);
        if (intentLabel != null)
        {
            intentLabel.text = text;
            intentLabel.enabled = show;
        }

        SetContainerActive(show);
        SetIcon(icon, show);
        SetTargetIcon(targetSprite, show);
        SetVisualAlpha(keepVisualHidden ? 0f : 1f);
    }

    public void SetVisualAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        CaptureBaseColorsIfNeeded();

        if (intentLabel != null)
        {
            Color labelColor = labelBaseColor;
            labelColor.a = labelBaseColor.a * alpha;
            intentLabel.color = labelColor;
        }

        if (intentIcon != null)
        {
            Color iconColor = iconBaseColor;
            iconColor.a = iconBaseColor.a * alpha;
            intentIcon.color = iconColor;
        }

        if (targetIcon != null)
            targetIcon.color = new Color(1f, 1f, 1f, alpha);
    }

    // Right after the text, in place of the target's name. The label keeps that much room free on
    // its right, so the centred text moves left and text and icon stay centred together.
    private void SetTargetIcon(Sprite sprite, bool show)
    {
        if (intentLabel == null)
            return;

        if (!show || sprite == null)
        {
            if (targetIcon != null)
                targetIcon.enabled = false;
            intentLabel.margin = labelBaseMargin;
            return;
        }

        Image image = EnsureTargetIcon();
        float size = intentLabel.fontSize * TargetIconScale;
        image.sprite = sprite;
        image.enabled = true;
        image.rectTransform.sizeDelta = new Vector2(size, size);

        intentLabel.margin = labelBaseMargin + new Vector4(0f, 0f, TargetIconGap + size, 0f);
        intentLabel.ForceMeshUpdate();
        Bounds textBounds = intentLabel.textBounds;
        image.rectTransform.anchoredPosition = new Vector2(textBounds.max.x + TargetIconGap + size * 0.5f, textBounds.center.y);
    }

    private Image EnsureTargetIcon()
    {
        if (targetIcon != null)
            return targetIcon;

        // A copy of the view (for an extra enemy or an ally) brings the original's icon along.
        Transform copied = intentLabel.transform.Find(TargetIconName);
        if (copied != null && copied.TryGetComponent(out targetIcon))
            return targetIcon;

        var iconObject = new GameObject(TargetIconName, typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(intentLabel.transform, false);

        targetIcon = iconObject.GetComponent<Image>();
        targetIcon.preserveAspect = true;
        targetIcon.raycastTarget = false;

        // Placed from the label's pivot, where the text's own coordinates start.
        RectTransform rect = targetIcon.rectTransform;
        rect.anchorMin = intentLabel.rectTransform.pivot;
        rect.anchorMax = intentLabel.rectTransform.pivot;
        rect.pivot = new Vector2(0.5f, 0.5f);
        return targetIcon;
    }

    private void SetIcon(Sprite icon, bool show)
    {
        if (intentIcon == null)
            return;

        intentIcon.sprite = icon;
        intentIcon.enabled = show && icon != null;
    }

    private void ClearIcon()
    {
        if (intentIcon == null)
            return;

        intentIcon.sprite = null;
        intentIcon.enabled = false;
    }

    private void SetContainerActive(bool active)
    {
        if (intentContainer == null)
            return;

        if (intentContainer == owner)
            return;

        intentContainer.SetActive(active);
    }

    // For a copy of the view: the original's own colours. The original may be faded out when it
    // is copied, and the copy would otherwise take those faded colours as its own.
    public void CopyBaseColors(EnemyIntentPresenter original)
    {
        original.CaptureBaseColorsIfNeeded();
        labelBaseColor = original.labelBaseColor;
        iconBaseColor = original.iconBaseColor;
        capturedBaseColors = true;
    }

    private void CaptureBaseColorsIfNeeded()
    {
        if (capturedBaseColors)
            return;

        if (intentLabel != null)
            labelBaseColor = intentLabel.color;
        if (intentIcon != null)
            iconBaseColor = intentIcon.color;
        capturedBaseColors = true;
    }
}
