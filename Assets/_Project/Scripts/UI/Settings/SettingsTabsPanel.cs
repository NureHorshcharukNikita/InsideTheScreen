using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsTabsPanel : MonoBehaviour
{
    private static readonly Color ActiveTabColor = new(0.14f, 0.29f, 0.25f, 1f);
    private static readonly Color InactiveTabColor = new(0.04f, 0.07f, 0.08f, 0.96f);
    private static readonly Color HighlightedTabColor = new(0.18f, 0.36f, 0.31f, 1f);
    private static readonly Color PressedTabColor = new(0.08f, 0.17f, 0.15f, 1f);
    private static readonly Color PauseActiveTabColor = new(0.42f, 0.28f, 0.22f, 1f);
    private static readonly Color PauseInactiveTabColor = new(0.20f, 0.12f, 0.10f, 0.96f);
    private static readonly Color PauseHighlightedTabColor = new(0.50f, 0.35f, 0.28f, 1f);
    private static readonly Color PausePressedTabColor = new(0.28f, 0.17f, 0.14f, 1f);
    private const int ContentTopPadding = 0;

    private Button controlsTabButton;
    private Button audioTabButton;
    private Transform controlsContent;
    private Transform audioContent;
    private Button resetButton;
    private ScrollRect scrollRect;
    private Scrollbar verticalScrollbar;
    private RectTransform settingsBody;
    private bool pauseStyle;
    private bool showingControls;

    public static void Ensure(Transform settingsRoot, Transform controlsContent, Button resetButton = null, Transform controlsTitle = null)
    {
        if (settingsRoot == null || controlsContent == null)
            return;

        SettingsTabsPanel tabs = settingsRoot.GetComponent<SettingsTabsPanel>();
        if (tabs == null)
            tabs = settingsRoot.gameObject.AddComponent<SettingsTabsPanel>();

        bool pauseSettings = IsPauseSettings(settingsRoot);
        RectTransform settingsBody = controlsContent.parent as RectTransform;
        AdjustScrollViewport(settingsBody);
        ApplyTopPadding(controlsContent);

        Transform audioContent = EnsureAudioContent(settingsRoot, controlsContent, settingsBody);
        AudioSettingsPanel.Ensure(audioContent, pauseSettings ? AudioSettingsPanel.VisualStyle.PauseMenu : AudioSettingsPanel.VisualStyle.MainMenu);

        RectTransform tabBar = EnsureTabBar(settingsRoot);
        TMP_Text referenceText = settingsRoot.GetComponentInChildren<TMP_Text>(true);
        Button controlsTab = EnsureTabButton(tabBar, "ControlsTab", "Controls", -128f, referenceText);
        Button audioTab = EnsureTabButton(tabBar, "AudioTab", "Audio", 128f, referenceText);

        if (controlsTitle != null)
            controlsTitle.gameObject.SetActive(false);

        tabs.Configure(controlsContent, audioContent, controlsTab, audioTab, resetButton, controlsContent.GetComponentInParent<ScrollRect>(), settingsBody, pauseSettings);
        AdjustScrollbar(tabs.scrollRect, pauseSettings);
    }

    private void Configure(Transform controls, Transform audio, Button controlsButton, Button audioButton, Button reset, ScrollRect scroll, RectTransform body, bool pause)
    {
        controlsContent = controls;
        audioContent = audio;
        controlsTabButton = controlsButton;
        audioTabButton = audioButton;
        resetButton = reset;
        scrollRect = scroll;
        settingsBody = body;
        pauseStyle = pause;
        verticalScrollbar = FindVerticalScrollbar(scrollRect);

        ApplyTabColors(controlsTabButton, false);
        ApplyTabColors(audioTabButton, false);

        controlsTabButton.onClick.RemoveListener(ShowControls);
        controlsTabButton.onClick.AddListener(ShowControls);
        audioTabButton.onClick.RemoveListener(ShowAudio);
        audioTabButton.onClick.AddListener(ShowAudio);

        ShowControls();
    }

    private void ShowControls()
    {
        SetTab(true);
    }

    private void ShowAudio()
    {
        SetTab(false);
    }

    private void LateUpdate()
    {
        if (showingControls && controlsContent is RectTransform controlsRect)
            UpdateScrollbarVisibility(controlsRect);
    }

    private void SetTab(bool showControls)
    {
        showingControls = showControls;

        if (settingsBody != null)
            settingsBody.gameObject.SetActive(showControls);

        if (audioContent != null)
            audioContent.gameObject.SetActive(!showControls);

        if (resetButton != null)
            resetButton.gameObject.SetActive(showControls);

        if (scrollRect != null && showControls)
        {
            RectTransform content = controlsContent as RectTransform;
            scrollRect.content = content;
            RefreshScrollLayout(content);
            ApplyPreferredContentHeight(content);
            UpdateScrollbarVisibility(content);
            scrollRect.verticalNormalizedPosition = 1f;
        }
        else if (scrollRect != null)
        {
            scrollRect.vertical = false;
            scrollRect.velocity = Vector2.zero;
            SetSettingsScrollbarsActive(false);
        }

        ApplyButtonColor(controlsTabButton, GetTabColor(showControls));
        ApplyButtonColor(audioTabButton, GetTabColor(!showControls));
    }

    private static Transform EnsureAudioContent(Transform settingsRoot, Transform controlsContent, RectTransform settingsBody)
    {
        Transform oldParent = controlsContent.parent;
        Transform existing = settingsRoot.Find("AudioContent") ?? oldParent.Find("AudioContent");
        if (existing != null)
        {
            if (existing.parent != settingsRoot)
                existing.SetParent(settingsRoot, false);

            ConfigureAudioContent(existing, controlsContent, settingsBody);
            return existing;
        }

        RectTransform audioRect = CreateUIObject("AudioContent", settingsRoot);
        ConfigureAudioContent(audioRect, controlsContent, settingsBody);
        audioRect.gameObject.SetActive(false);
        return audioRect;
    }

    private static void ConfigureAudioContent(Transform audioContent, Transform controlsContent, RectTransform settingsBody)
    {
        RectTransform audioRect = audioContent as RectTransform;
        if (audioRect == null)
            return;

        if (settingsBody != null)
        {
            float height = AudioSettingsPanel.PreferredBlockHeight;
            float top = settingsBody.anchoredPosition.y + settingsBody.sizeDelta.y * (1f - settingsBody.pivot.y);
            audioRect.anchorMin = settingsBody.anchorMin;
            audioRect.anchorMax = settingsBody.anchorMax;
            audioRect.pivot = new Vector2(settingsBody.pivot.x, 1f);
            audioRect.anchoredPosition = new Vector2(settingsBody.anchoredPosition.x, top);
            audioRect.sizeDelta = new Vector2(settingsBody.sizeDelta.x, height);
        }

        VerticalLayoutGroup sourceLayout = controlsContent.GetComponent<VerticalLayoutGroup>();
        VerticalLayoutGroup layout = audioRect.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = audioRect.gameObject.AddComponent<VerticalLayoutGroup>();

        layout.padding = sourceLayout != null ? CopyPaddingWithTop(sourceLayout.padding) : new RectOffset(20, 38, ContentTopPadding, 0);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 0f;

        ContentSizeFitter fitter = audioRect.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = audioRect.gameObject.AddComponent<ContentSizeFitter>();

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void ApplyTopPadding(Transform content)
    {
        VerticalLayoutGroup layout = content != null ? content.GetComponent<VerticalLayoutGroup>() : null;
        if (layout == null)
            return;

        layout.padding = CopyPaddingWithTop(layout.padding);
    }

    private static RectOffset CopyPaddingWithTop(RectOffset padding)
    {
        if (padding == null)
            return new RectOffset(20, 38, ContentTopPadding, 0);

        return new RectOffset(padding.left, padding.right, ContentTopPadding, padding.bottom);
    }

    private static RectTransform EnsureTabBar(Transform settingsRoot)
    {
        Transform existing = settingsRoot.Find("SettingsTabs");
        RectTransform tabBar = existing != null ? (RectTransform)existing : CreateUIObject("SettingsTabs", settingsRoot);

        bool pauseSettings = IsPauseSettings(settingsRoot);
        tabBar.anchorMin = pauseSettings ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
        tabBar.anchorMax = tabBar.anchorMin;
        tabBar.pivot = new Vector2(0.5f, 0.5f);
        tabBar.anchoredPosition = pauseSettings ? new Vector2(0f, 175f) : new Vector2(0f, -145f);
        tabBar.sizeDelta = new Vector2(520f, 44f);
        return tabBar;
    }

    private static void AdjustScrollViewport(RectTransform viewport)
    {
        if (viewport == null)
            return;

        viewport.anchoredPosition = new Vector2(0f, -30f);
        viewport.sizeDelta = new Vector2(540f, 330f);
    }

    private static void RefreshScrollLayout(RectTransform content)
    {
        if (content == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
    }

    private static void ApplyPreferredContentHeight(RectTransform content)
    {
        if (content == null)
            return;

        float preferredHeight = LayoutUtility.GetPreferredHeight(content);
        if (preferredHeight <= 0f)
            return;

        content.sizeDelta = new Vector2(content.sizeDelta.x, preferredHeight);
        RefreshScrollLayout(content);
    }

    private void UpdateScrollbarVisibility(RectTransform content)
    {
        if (scrollRect == null)
            return;

        if (verticalScrollbar == null)
            verticalScrollbar = FindVerticalScrollbar(scrollRect);

        bool canScroll = CanScroll(scrollRect, content);
        scrollRect.vertical = canScroll;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        if (verticalScrollbar != null)
            scrollRect.verticalScrollbar = verticalScrollbar;

        SetSettingsScrollbarsActive(canScroll);

        if (!canScroll)
            scrollRect.velocity = Vector2.zero;
    }

    private static Scrollbar FindVerticalScrollbar(ScrollRect target)
    {
        if (target == null)
            return null;

        if (target.verticalScrollbar != null)
            return target.verticalScrollbar;

        foreach (Scrollbar scrollbar in target.GetComponentsInChildren<Scrollbar>(true))
        {
            if (scrollbar.direction == Scrollbar.Direction.BottomToTop || scrollbar.direction == Scrollbar.Direction.TopToBottom)
                return scrollbar;
        }

        return null;
    }

    private void SetSettingsScrollbarsActive(bool active)
    {
        if (verticalScrollbar == null)
            verticalScrollbar = FindVerticalScrollbar(scrollRect);

        if (verticalScrollbar != null)
            verticalScrollbar.gameObject.SetActive(active);
    }

    private static bool CanScroll(ScrollRect target, RectTransform content)
    {
        if (target == null || content == null)
            return false;

        RectTransform viewport = target.viewport != null ? target.viewport : target.GetComponent<RectTransform>();
        if (viewport == null)
            return false;

        Canvas.ForceUpdateCanvases();
        float contentHeight = GetRequiredContentHeight(content);

        return contentHeight > viewport.rect.height + 1f;
    }

    private static float GetRequiredContentHeight(RectTransform content)
    {
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            float preferredHeight = LayoutUtility.GetPreferredHeight(content);
            return preferredHeight > 0f ? preferredHeight : content.rect.height;
        }

        float height = layout.padding.top + layout.padding.bottom;
        int activeChildren = 0;

        for (int i = 0; i < content.childCount; i++)
        {
            RectTransform child = content.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
                continue;

            if (activeChildren > 0)
                height += layout.spacing;

            height += GetPreferredChildHeight(child);
            activeChildren++;
        }

        return height;
    }

    private static float GetPreferredChildHeight(RectTransform child)
    {
        float preferredHeight = LayoutUtility.GetPreferredHeight(child);
        if (preferredHeight > 0f)
            return preferredHeight;

        LayoutElement layoutElement = child.GetComponent<LayoutElement>();
        if (layoutElement != null && layoutElement.preferredHeight > 0f)
            return layoutElement.preferredHeight;

        return child.rect.height;
    }

    private static void AdjustScrollbar(ScrollRect scrollRect, bool pauseSettings)
    {
        if (scrollRect == null || scrollRect.verticalScrollbar == null)
            return;

        RectTransform scrollbarRect = scrollRect.verticalScrollbar.GetComponent<RectTransform>();
        if (scrollbarRect == null)
            return;

        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 1f);
        float rightOffset = pauseSettings ? -18f : -16f;
        scrollbarRect.offsetMin = new Vector2(rightOffset - 10f, 0f);
        scrollbarRect.offsetMax = new Vector2(rightOffset, 0f);
    }

    private static Button EnsureTabButton(RectTransform tabBar, string name, string label, float x, TMP_Text referenceText)
    {
        Transform existing = tabBar.Find(name);
        if (existing != null && existing.TryGetComponent(out Button existingButton))
            return existingButton;

        RectTransform rect = CreateUIObject(name, tabBar);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(246f, 42f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = InactiveTabColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = InactiveTabColor;
        colors.highlightedColor = HighlightedTabColor;
        colors.pressedColor = PressedTabColor;
        colors.selectedColor = HighlightedTabColor;
        colors.disabledColor = new Color(InactiveTabColor.r, InactiveTabColor.g, InactiveTabColor.b, 0.45f);
        button.colors = colors;

        TMP_Text text = CreateText("Label", rect, label, referenceText);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        return button;
    }

    private void ApplyTabColors(Button button, bool active)
    {
        if (button == null)
            return;

        Color normalColor = GetTabColor(active);
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = pauseStyle ? PauseHighlightedTabColor : HighlightedTabColor;
        colors.pressedColor = pauseStyle ? PausePressedTabColor : PressedTabColor;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.45f);
        button.colors = colors;

        if (button.targetGraphic != null)
            button.targetGraphic.color = normalColor;
    }

    private Color GetTabColor(bool active)
    {
        if (pauseStyle)
            return active ? PauseActiveTabColor : PauseInactiveTabColor;

        return active ? ActiveTabColor : InactiveTabColor;
    }

    private static bool IsPauseSettings(Transform settingsRoot)
    {
        return settingsRoot != null && settingsRoot.Find("OptionsTitle") != null;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, TMP_Text referenceText)
    {
        RectTransform rect = CreateUIObject(name, parent);
        TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (referenceText != null)
        {
            text.font = referenceText.font;
            text.fontMaterial = referenceText.fontMaterial;
        }

        text.text = value;
        text.fontSize = 24f;
        text.color = new Color(0.92f, 0.95f, 0.88f, 1f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = parent.gameObject.layer;
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static void ApplyButtonColor(Button button, Color color)
    {
        if (button == null)
            return;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        button.colors = colors;

        if (button.targetGraphic != null)
            button.targetGraphic.color = color;
    }

}
