using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsAudioRow : MonoBehaviour
{
    [SerializeField] private AudioChannel channel;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text percentLabel;

    private void Awake()
    {
        if (slider != null)
            slider.onValueChanged.AddListener(SetVolume);

        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        float value = AudioManager.Instance != null
            ? AudioManager.Instance.GetVolume(channel)
            : 1f;

        if (slider != null)
            slider.SetValueWithoutNotify(value);

        UpdatePercent(value);
    }

    private void SetVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume(channel, value);

        UpdatePercent(value);
    }

    private void UpdatePercent(float value)
    {
        if (percentLabel != null)
            percentLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
