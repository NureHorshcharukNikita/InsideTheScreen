using UnityEngine;

public partial class EnemyIntentView
{
    private void ClearIcon()
    {
        if (intentIcon == null)
            return;

        intentIcon.sprite = null;
        intentIcon.enabled = false;
    }

    private void SetIntentContainerActive(bool active)
    {
        if (intentContainer == null)
            return;

        if (intentContainer == gameObject)
            return;

        intentContainer.SetActive(active);
    }

    private void CaptureBaseColorsIfNeeded()
    {
        if (_capturedBaseColors)
            return;

        if (intentLabel != null)
            _labelBaseColor = intentLabel.color;
        if (intentIcon != null)
            _iconBaseColor = intentIcon.color;
        _capturedBaseColors = true;
    }

    private void SetIntentVisualAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        CaptureBaseColorsIfNeeded();

        if (intentLabel != null)
        {
            Color c = _labelBaseColor;
            c.a = _labelBaseColor.a * alpha;
            intentLabel.color = c;
        }

        if (intentIcon != null)
        {
            Color c = _iconBaseColor;
            c.a = _iconBaseColor.a * alpha;
            intentIcon.color = c;
        }
    }

}
