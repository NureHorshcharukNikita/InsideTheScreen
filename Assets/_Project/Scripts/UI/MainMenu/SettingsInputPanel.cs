using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsInputPanel : MonoBehaviour
{
    [SerializeField] private SettingsKeyBindingRow[] bindingRows;
    [SerializeField] private Button resetButton;

    private void Awake()
    {
        EnsureAudioSettings();

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetBindings);

        Refresh();
    }

    private void OnEnable()
    {
        EnsureAudioSettings();

        GameInputSettings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameInputSettings.Changed -= Refresh;
    }

    public void ResetBindings()
    {
        GameInputSettings.ResetToDefaults();
    }

    public void Refresh()
    {
        if (bindingRows == null)
            return;

        foreach (SettingsKeyBindingRow row in bindingRows)
        {
            if (row != null)
                row.Refresh();
        }
    }

    private void EnsureAudioSettings()
    {
        Transform content = transform.Find("SettingsBody/BindingsContent");
        Transform controlsTitle = transform.Find("ControlsTitle");
        SettingsTabsPanel.Ensure(transform, content, resetButton, controlsTitle);
    }
}
