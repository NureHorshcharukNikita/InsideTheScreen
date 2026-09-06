using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class AudioSettingsPanel
{
    public enum VisualStyle
    {
        MainMenu,
        PauseMenu
    }

    private static readonly AudioChannel[] Channels =
    {
        AudioChannel.Master,
        AudioChannel.Music,
        AudioChannel.Ambience,
        AudioChannel.Sfx,
        AudioChannel.Ui
    };

    private const string BlockName = "AudioSettingsBlock";
    private const float RowHeight = 44f;
    private const float RowSpacing = 18f;
    public static float PreferredBlockHeight => Channels.Length * RowHeight + (Channels.Length - 1) * RowSpacing;
    private static readonly Color MainRowColor = new(0.09f, 0.22f, 0.18f, 0.96f);
    private static readonly Color MainSliderBackgroundColor = new(0.04f, 0.08f, 0.07f, 0.95f);
    private static readonly Color MainSliderFillColor = new(0.35f, 0.78f, 0.55f, 1f);
    private static readonly Color PauseRowColor = new(0.32f, 0.22f, 0.18f, 0.96f);
    private static readonly Color PauseSliderBackgroundColor = new(0.18f, 0.11f, 0.09f, 0.95f);
    private static readonly Color PauseSliderFillColor = new(0.64f, 0.45f, 0.34f, 1f);
    private static readonly Color HandleColor = new(0.92f, 0.95f, 0.88f, 1f);

    public static void Ensure(Transform parent, VisualStyle style = VisualStyle.MainMenu)
    {
        if (parent == null)
            return;

        TMP_Text referenceText = parent.GetComponentInParent<TMP_Text>(true)
            ?? parent.GetComponentInChildren<TMP_Text>(true);

        Transform existingBlock = parent.Find(BlockName);
        if (existingBlock != null)
        {
            ConfigureBlock(existingBlock as RectTransform, referenceText, style);
            RefreshSliders(parent);
            return;
        }

        RectTransform block = CreateUIObject(BlockName, parent);
        ConfigureBlock(block, referenceText, style);
        RefreshSliders(parent);
    }

    private static void ConfigureBlock(RectTransform block, TMP_Text referenceText, VisualStyle style)
    {
        if (block == null)
            return;

        VerticalLayoutGroup layout = block.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = block.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = RowSpacing;

        LayoutElement blockLayout = block.GetComponent<LayoutElement>();
        if (blockLayout == null)
            blockLayout = block.gameObject.AddComponent<LayoutElement>();

        blockLayout.preferredHeight = PreferredBlockHeight;
        ApplyBlockHeight(block);

        foreach (AudioChannel channel in Channels)
            EnsureVolumeRow(block, referenceText, channel, style);
    }

    private static void ApplyBlockHeight(Transform block)
    {
        if (block == null)
            return;

        LayoutElement blockLayout = block.GetComponent<LayoutElement>();
        if (blockLayout != null)
            blockLayout.preferredHeight = PreferredBlockHeight;

        RectTransform blockRect = block as RectTransform;
        if (blockRect != null)
            blockRect.sizeDelta = new Vector2(blockRect.sizeDelta.x, PreferredBlockHeight);
    }

    private static void EnsureVolumeRow(RectTransform parent, TMP_Text referenceText, AudioChannel channel, VisualStyle style)
    {
        RectTransform row = parent.Find($"AudioVolume_{channel}") as RectTransform;
        if (row == null)
            row = CreateUIObject($"AudioVolume_{channel}", parent);

        ConfigureVolumeRow(row, referenceText, channel, style);
    }

    private static void ConfigureVolumeRow(RectTransform row, TMP_Text referenceText, AudioChannel channel, VisualStyle style)
    {
        Image rowImage = row.GetComponent<Image>();
        if (rowImage == null)
            rowImage = row.gameObject.AddComponent<Image>();

        rowImage.color = GetRowColor(style);

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        if (rowLayout == null)
            rowLayout = row.gameObject.AddComponent<LayoutElement>();

        rowLayout.preferredHeight = RowHeight;

        EnsureLabel(row, "ChannelLabel", referenceText, GetChannelLabel(channel), TextAlignmentOptions.MidlineLeft, new Vector2(18f, 0f), new Vector2(150f, 0f));

        Slider slider = row.Find("Slider")?.GetComponent<Slider>();
        if (slider == null)
            slider = CreateSlider(row, style);
        else
            ConfigureSlider(slider, style);

        float volume = GetVolume(channel);
        TMP_Text valueLabel = EnsureLabel(row, "ValueLabel", referenceText, FormatPercent(volume), TextAlignmentOptions.MidlineRight, new Vector2(-18f, 0f), new Vector2(70f, 0f));
        slider.SetValueWithoutNotify(volume);
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(value =>
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetVolume(channel, value);

            if (valueLabel != null)
                valueLabel.text = FormatPercent(value);
        });
    }

    private static Slider CreateSlider(RectTransform row, VisualStyle style)
    {
        RectTransform sliderRect = CreateUIObject("Slider", row);
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.offsetMin = new Vector2(165f, -9f);
        sliderRect.offsetMax = new Vector2(-92f, 9f);

        Slider slider = sliderRect.gameObject.AddComponent<Slider>();

        RectTransform background = CreateUIObject("Background", sliderRect);
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.offsetMin = Vector2.zero;
        background.offsetMax = Vector2.zero;
        background.gameObject.AddComponent<Image>().color = GetSliderBackgroundColor(style);

        RectTransform fillArea = CreateUIObject("Fill Area", sliderRect);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(0f, 0f);
        fillArea.offsetMax = new Vector2(0f, 0f);

        RectTransform fill = CreateUIObject("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = GetSliderFillColor(style);

        RectTransform handleArea = CreateUIObject("Handle Slide Area", sliderRect);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = Vector2.zero;
        handleArea.offsetMax = Vector2.zero;

        RectTransform handle = CreateUIObject("Handle", handleArea);
        handle.sizeDelta = new Vector2(18f, 24f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = HandleColor;

        ConfigureSlider(slider, style);

        return slider;
    }

    private static void ConfigureSlider(Slider slider, VisualStyle style)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        if (sliderRect != null)
        {
            sliderRect.anchorMin = new Vector2(0f, 0.5f);
            sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.offsetMin = new Vector2(165f, -9f);
            sliderRect.offsetMax = new Vector2(-92f, 9f);
        }

        Image background = slider.transform.Find("Background")?.GetComponent<Image>();
        if (background != null)
            background.color = GetSliderBackgroundColor(style);

        Transform fill = slider.transform.Find("Fill Area/Fill");
        if (fill != null)
        {
            slider.fillRect = fill as RectTransform;
            Image fillImage = fill.GetComponent<Image>();
            if (fillImage != null)
                fillImage.color = GetSliderFillColor(style);
        }

        Transform handle = slider.transform.Find("Handle Slide Area/Handle");
        if (handle != null)
        {
            slider.handleRect = handle as RectTransform;
            Image handleImage = handle.GetComponent<Image>();
            if (handleImage != null)
            {
                handleImage.color = HandleColor;
                slider.targetGraphic = handleImage;
            }
        }
    }

    private static TMP_Text EnsureLabel(RectTransform parent, string name, TMP_Text referenceText, string text, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        TMP_Text label = parent.Find(name)?.GetComponent<TMP_Text>();
        return label != null
            ? ConfigureLabel(label, referenceText, text, alignment, anchoredPosition, sizeDelta)
            : CreateLabel(parent, name, referenceText, text, alignment, anchoredPosition, sizeDelta);
    }

    private static TMP_Text CreateLabel(RectTransform parent, string name, TMP_Text referenceText, string text, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform rect = CreateUIObject(name, parent);

        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        return ConfigureLabel(label, referenceText, text, alignment, anchoredPosition, sizeDelta);
    }

    private static TMP_Text ConfigureLabel(TMP_Text label, TMP_Text referenceText, string text, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform rect = label.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(alignment == TextAlignmentOptions.MidlineRight ? 1f : 0f, 0f);
            rect.anchorMax = new Vector2(alignment == TextAlignmentOptions.MidlineRight ? 1f : 0f, 1f);
            rect.pivot = new Vector2(alignment == TextAlignmentOptions.MidlineRight ? 1f : 0f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        CopyTextStyle(referenceText, label);
        label.text = text;
        label.fontSize = 21f;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static void CopyTextStyle(TMP_Text referenceText, TMP_Text target)
    {
        if (referenceText != null)
        {
            if (referenceText.font != null)
                target.font = referenceText.font;

            if (referenceText.fontSharedMaterial != null)
                target.fontSharedMaterial = referenceText.fontSharedMaterial;
        }

        target.color = new Color(0.92f, 0.95f, 0.88f, 1f);
    }

    private static void RefreshSliders(Transform root)
    {
        if (root == null)
            return;

        foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
        {
            Transform row = FindAudioRow(slider.transform);
            AudioChannel? channel = TryGetChannelFromRow(row);
            if (!channel.HasValue)
                continue;

            float volume = GetVolume(channel.Value);
            slider.SetValueWithoutNotify(volume);

            TMP_Text valueLabel = row != null ? row.Find("ValueLabel")?.GetComponent<TMP_Text>() : null;
            if (valueLabel != null)
                valueLabel.text = FormatPercent(volume);
        }
    }

    private static Transform FindAudioRow(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name.StartsWith("AudioVolume_"))
                return current;

            current = current.parent;
        }

        return null;
    }

    private static AudioChannel? TryGetChannelFromRow(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name.StartsWith("AudioVolume_") && System.Enum.TryParse(current.name.Substring("AudioVolume_".Length), out AudioChannel channel))
                return channel;

            current = current.parent;
        }

        return null;
    }

    private static float GetVolume(AudioChannel channel)
    {
        return AudioManager.Instance != null ? AudioManager.Instance.GetVolume(channel) : 1f;
    }

    private static string GetChannelLabel(AudioChannel channel)
    {
        return channel switch
        {
            AudioChannel.Master => "Master",
            AudioChannel.Music => "Music",
            AudioChannel.Ambience => "Ambience",
            AudioChannel.Sfx => "SFX",
            AudioChannel.Ui => "UI",
            _ => channel.ToString()
        };
    }

    private static Color GetRowColor(VisualStyle style)
    {
        return style == VisualStyle.PauseMenu ? PauseRowColor : MainRowColor;
    }

    private static Color GetSliderBackgroundColor(VisualStyle style)
    {
        return style == VisualStyle.PauseMenu ? PauseSliderBackgroundColor : MainSliderBackgroundColor;
    }

    private static Color GetSliderFillColor(VisualStyle style)
    {
        return style == VisualStyle.PauseMenu ? PauseSliderFillColor : MainSliderFillColor;
    }

    private static string FormatPercent(float value)
    {
        return $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
