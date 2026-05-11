using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class EnemyIntentPresenter
{
    private readonly GameObject owner;
    private readonly TMP_Text intentLabel;
    private readonly Image intentIcon;
    private readonly GameObject intentContainer;

    private Color labelBaseColor = Color.white;
    private Color iconBaseColor = Color.white;
    private bool capturedBaseColors;

    public EnemyIntentPresenter(GameObject owner, TMP_Text intentLabel, Image intentIcon, GameObject intentContainer)
    {
        this.owner = owner;
        this.intentLabel = intentLabel;
        this.intentIcon = intentIcon;
        this.intentContainer = intentContainer;
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
        SetVisualAlpha(1f);
    }

    public void ShowIntent(string text, Sprite icon, bool keepVisualHidden)
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
