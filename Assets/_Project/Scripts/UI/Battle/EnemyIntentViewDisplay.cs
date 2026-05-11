using UnityEngine;

public partial class EnemyIntentView
{
    private void Refresh(bool keepVisualHidden = false)
    {
        ResolveBrainIfMissing();
        ResolveTargetCharacterIfMissing();
        SubscribeToBrain();

        if (intentLabel == null)
            return;

        CaptureBaseColorsIfNeeded();

        if (brain == null)
        {
            intentLabel.text = "";
            SetIntentContainerActive(false);
            intentLabel.enabled = false;
            ClearIcon();
            SetIntentVisualAlpha(1f);
            return;
        }

        string text = brain.CurrentPlan.HasAbility
            ? brain.CurrentPlan.GetIntentLabel()
            : NoPlanPlaceholder;

        intentLabel.text = text;

        bool show = !string.IsNullOrEmpty(text);
        SetIntentContainerActive(show);
        intentLabel.enabled = show;

        if (intentIcon != null)
        {
            Sprite s = brain.CurrentPlan.Ability != null ? brain.CurrentPlan.Ability.icon : null;
            intentIcon.sprite = s;
            intentIcon.enabled = show && s != null;
        }

        SetIntentVisualAlpha(keepVisualHidden ? 0f : 1f);
    }
}
