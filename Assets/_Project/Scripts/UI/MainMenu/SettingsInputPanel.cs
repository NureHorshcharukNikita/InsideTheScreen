using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsInputPanel : MonoBehaviour
{
    [SerializeField] private SettingsKeyBindingRow[] bindingRows;
    [SerializeField] private SettingsAudioRow[] audioRows;
    [SerializeField] private Button controlsTabButton;
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private TMP_Text sectionTitle;
    [SerializeField] private GameObject controlsContent;
    [SerializeField] private GameObject audioContent;
    [SerializeField] private Image controlsTabImage;
    [SerializeField] private Image audioTabImage;
    [SerializeField] private Sprite controlsNormalSprite;
    [SerializeField] private Sprite controlsActiveSprite;
    [SerializeField] private Sprite audioNormalSprite;
    [SerializeField] private Sprite audioActiveSprite;
    [SerializeField] private Image actionButtonImage;
    [SerializeField] private Sprite resetButtonSprite;
    [SerializeField] private ScrollRect controlsScrollRect;
    [SerializeField] private ScrollRect audioScrollRect;

    private bool controlsActive;

    private void Awake()
    {
        if (controlsTabButton != null)
            controlsTabButton.onClick.AddListener(ShowControls);

        if (audioTabButton != null)
            audioTabButton.onClick.AddListener(ShowAudio);

        if (applyButton != null)
            applyButton.onClick.AddListener(HandleActionButton);

        Refresh();
    }

    private void OnEnable()
    {
        GameInputSettings.Changed += Refresh;
        ResetToDefaultView();
        Refresh();
    }

    private void OnDisable()
    {
        GameInputSettings.Changed -= Refresh;
    }

    public void ShowControls()
    {
        SetActiveTab(true);
        ResetScrollPosition(controlsScrollRect);
    }

    public void ShowAudio()
    {
        SetActiveTab(false);
        ResetScrollPosition(audioScrollRect);
    }

    public void ResetToDefaultView()
    {
        SetActiveTab(true);
        ResetAllScrollPositions();
        StartCoroutine(ResetScrollPositionsAfterLayout());
    }

    public void ResetAudio()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.ResetVolumes();
        Refresh();
    }

    public void ResetControls()
    {
        GameInputSettings.ResetToDefaults();
        Refresh();
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

        if (audioRows == null)
            return;

        foreach (SettingsAudioRow row in audioRows)
        {
            if (row != null)
                row.Refresh();
        }
    }

    private void SetActiveTab(bool controlsActive)
    {
        this.controlsActive = controlsActive;

        if (sectionTitle != null)
            sectionTitle.text = controlsActive ? "Keyboard Bindings" : "Audio Settings";

        if (controlsContent != null)
            controlsContent.SetActive(controlsActive);

        if (audioContent != null)
            audioContent.SetActive(!controlsActive);

        if (controlsTabImage != null)
            controlsTabImage.sprite = controlsActive ? controlsActiveSprite : controlsNormalSprite;

        if (audioTabImage != null)
            audioTabImage.sprite = controlsActive ? audioNormalSprite : audioActiveSprite;

        if (actionButtonImage != null)
            actionButtonImage.sprite = resetButtonSprite;
    }

    private void HandleActionButton()
    {
        if (controlsActive)
        {
            ResetControls();
            return;
        }

        ResetAudio();
    }

    private void ResetAllScrollPositions()
    {
        Canvas.ForceUpdateCanvases();
        ResetScrollPosition(controlsScrollRect);
        ResetScrollPosition(audioScrollRect);
    }

    private IEnumerator ResetScrollPositionsAfterLayout()
    {
        yield return null;
        ResetAllScrollPositions();
    }

    private static void ResetScrollPosition(ScrollRect scrollRect)
    {
        if (scrollRect == null)
            return;

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
