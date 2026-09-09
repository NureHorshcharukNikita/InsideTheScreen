using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryQuestPageHierarchyBuilder
{
    private const string InventoryPrefabPath = "Assets/_Project/Prefabs/UI/InventoryWindow.prefab";
    private const int BuilderVersion = 27;
    private const int QuestPageIndex = 2;

    private static readonly string[] SampleQuestPaths =
    {
        "Assets/_Project/ScriptableObjects/Quests/Q_TraderKimParcel.asset",
        "Assets/_Project/ScriptableObjects/Quests/Q_MysteriousChest.asset",
        "Assets/_Project/ScriptableObjects/Quests/Q_TraderKimSword.asset",
        "Assets/_Project/ScriptableObjects/Quests/Q_Parcel.asset"
    };

    private static readonly Color TransparentColor = new(1f, 1f, 1f, 0f);
    private static readonly Color HeaderColor = new(0.43137255f, 0.31764707f, 0.28627452f, 1f);
    private static readonly Color ScrollPanelColor = new(0.87058824f, 0.81960785f, 0.81960785f, 0.23529412f);
    private static readonly Color ItemColor = new(0.5568628f, 0.5058824f, 0.4862745f, 0.392f);
    private static readonly Color QuestListItemColor = new(0.59607846f, 0.48235294f, 0.44313726f, 0.78f);
    private static readonly Color SelectedColor = new(0.27450982f, 0.46666667f, 0.59607846f, 1f);
    private static readonly Color ScrollbarTrackColor = new(0.3137255f, 0.24313726f, 0.21568628f, 1f);
    private static readonly Color ScrollbarHandleColor = new(0.7254902f, 0.654902f, 0.61960787f, 1f);
    private static readonly Color TextColor = Color.black;
    private static readonly Color BorderColor = Color.black;

    [InitializeOnLoadMethod]
    private static void QueueAutoBuild()
    {
        EditorApplication.delayCall -= AutoBuild;
        EditorApplication.delayCall += AutoBuild;
        EditorApplication.playModeStateChanged -= RebuildAfterPlayMode;
        EditorApplication.playModeStateChanged += RebuildAfterPlayMode;
    }

    [MenuItem("Tools/Inside The Screen/UI/Rebuild Quest Page")]
    public static void BuildPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(InventoryPrefabPath);
        if (prefabRoot == null)
            return;

        try
        {
            InventoryWindow window = prefabRoot.GetComponent<InventoryWindow>();
            Transform content = prefabRoot.transform.Find("Content");
            if (window == null || content == null)
                return;

            GameObject questPage = GetOrCreateChild(content, "QuestsPage");
            QuestPageView view = questPage.GetComponent<QuestPageView>();
            if (IsCurrent(view) && IsQuestPageAssigned(window, questPage))
                return;

            ClearChildren(questPage.transform);
            ConfigureQuestPage(questPage, window);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, InventoryPrefabPath);
            Debug.Log("Inventory quest page hierarchy was rebuilt.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void AutoBuild()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            BuildPrefab();
    }

    private static void RebuildAfterPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        EditorApplication.delayCall -= AutoBuild;
        EditorApplication.delayCall += AutoBuild;
    }

    private static void ConfigureQuestPage(GameObject page, InventoryWindow window)
    {
        page.SetActive(false);

        RectTransform pageRect = GetOrAdd<RectTransform>(page);
        Stretch(pageRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image pageImage = GetOrAdd<Image>(page);
        pageImage.color = TransparentColor;
        pageImage.raycastTarget = false;

        TMP_Text fontSource = page.GetComponentInParent<InventoryWindow>().GetComponentInChildren<TMP_Text>(true);

        List<QuestData> sampleQuests = LoadSampleQuests();

        GameObject board = CreatePanel(page.transform, "QuestBoard", TransparentColor, false);
        RectTransform boardRect = GetOrAdd<RectTransform>(board);
        Stretch(boardRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        HorizontalLayoutGroup boardLayout = GetOrAdd<HorizontalLayoutGroup>(board);
        boardLayout.padding = new RectOffset(160, 160, 0, 0);
        boardLayout.spacing = 8;
        boardLayout.childAlignment = TextAnchor.UpperCenter;
        boardLayout.childControlWidth = true;
        boardLayout.childControlHeight = true;
        boardLayout.childForceExpandWidth = true;
        boardLayout.childForceExpandHeight = true;

        GameObject detailsPanel = CreatePanel(board.transform, "QuestDetailsPanel", TransparentColor, false);
        AddLayoutElement(detailsPanel, -1f, -1f, 13f, -1f);
        VerticalLayoutGroup detailsLayout = GetOrAdd<VerticalLayoutGroup>(detailsPanel);
        detailsLayout.padding = new RectOffset(0, 0, 0, 0);
        detailsLayout.spacing = 5;
        detailsLayout.childAlignment = TextAnchor.UpperCenter;
        detailsLayout.childControlWidth = true;
        detailsLayout.childControlHeight = true;
        detailsLayout.childForceExpandWidth = true;
        detailsLayout.childForceExpandHeight = false;

        CreateHeader(detailsPanel.transform, "QuestDetailsHeader", "Quest Details", fontSource);

        ScrollRect detailsScroll = CreateDetailsScroll(detailsPanel.transform, out RectTransform detailsContent);
        TMP_Text emptyDetailsText = CreateEmptyDetailsText(detailsScroll, fontSource);

        GameObject textColumn = GetOrCreateChild(detailsContent, "QuestTextColumn");
        VerticalLayoutGroup textLayout = GetOrAdd<VerticalLayoutGroup>(textColumn);
        textLayout.padding = new RectOffset(12, 0, 0, 0);
        textLayout.spacing = 5;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;
        AddLayoutElement(textColumn, -1f, -1f, 1f, 1f);
        SetMinLayoutElement(textColumn, 150f, -1f);

        TMP_Text titleText = CreateText(textColumn.transform, "QuestTitleText", string.Empty, 36f, TextAlignmentOptions.Left, fontSource, TextColor);
        AddLayoutElement(titleText.gameObject, -1f, 50f, -1f, 0f);

        TMP_Text giverText = CreateText(textColumn.transform, "QuestGiverText", string.Empty, 22f, TextAlignmentOptions.Left, fontSource, TextColor);
        giverText.textWrappingMode = TextWrappingModes.NoWrap;
        AddLayoutElement(giverText.gameObject, -1f, 32f, -1f, 0f);

        TMP_Text descriptionText = CreateText(textColumn.transform, "QuestDescriptionText", string.Empty, 26f, TextAlignmentOptions.TopLeft, fontSource, TextColor);
        descriptionText.textWrappingMode = TextWrappingModes.Normal;
        AddLayoutElement(descriptionText.gameObject, -1f, -1f, 1f, 1f);

        GameObject rewardRoot = CreatePanel(detailsContent, "QuestRewardPanel", ItemColor, false);
        VerticalLayoutGroup rewardLayout = GetOrAdd<VerticalLayoutGroup>(rewardRoot);
        rewardLayout.padding = new RectOffset(10, 10, 10, 10);
        rewardLayout.spacing = 8;
        rewardLayout.childAlignment = TextAnchor.UpperCenter;
        rewardLayout.childControlWidth = true;
        rewardLayout.childControlHeight = true;
        rewardLayout.childForceExpandWidth = true;
        rewardLayout.childForceExpandHeight = false;
        AddLayoutElement(rewardRoot, 240f, -1f, 0f, 1f);
        SetMinLayoutElement(rewardRoot, 220f, -1f);

        TMP_Text rewardTitle = CreateText(rewardRoot.transform, "RewardTitleText", "Reward", 30f, TextAlignmentOptions.Center, fontSource, TextColor);
        rewardTitle.textWrappingMode = TextWrappingModes.NoWrap;
        AddLayoutElement(rewardTitle.gameObject, -1f, 42f, -1f, 0f);

        Image rewardIcon = CreateImage(rewardRoot.transform, "RewardIcon", Color.white);
        rewardIcon.preserveAspect = true;
        rewardIcon.enabled = false;
        AddLayoutElement(rewardIcon.gameObject, 140f, 140f, 0f, 0f);

        TMP_Text rewardLabel = CreateText(rewardRoot.transform, "RewardLabelText", string.Empty, 20f, TextAlignmentOptions.Center, fontSource, TextColor);
        rewardLabel.textWrappingMode = TextWrappingModes.NoWrap;
        AddLayoutElement(rewardLabel.gameObject, -1f, 34f, -1f, 0f);

        GameObject listPanel = CreatePanel(board.transform, "QuestListPanel", TransparentColor, false);
        AddLayoutElement(listPanel, -1f, -1f, 7f, -1f);
        VerticalLayoutGroup listLayout = GetOrAdd<VerticalLayoutGroup>(listPanel);
        listLayout.padding = new RectOffset(0, 0, 0, 0);
        listLayout.spacing = 5;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        CreateHeader(listPanel.transform, "QuestListHeader", "Quests", fontSource);

        ScrollRect listScroll = CreateScroll(listPanel.transform, "QuestListScroll", "QuestListContent", ScrollPanelColor, -1f, out RectTransform listContent);
        TMP_Text emptyListText = CreateEmptyListText(listScroll, fontSource);
        QuestListItemView itemTemplate = CreateQuestItemTemplate(listContent, fontSource);
        itemTemplate.gameObject.SetActive(false);
        CreatePreviewQuestItems(listContent, itemTemplate, sampleQuests);
        SetQuestDetailsPreview(sampleQuests.Count > 0 ? sampleQuests[0] : null, titleText, giverText, descriptionText, rewardRoot, rewardLabel, rewardIcon);
        listContent.gameObject.SetActive(false);
        detailsContent.gameObject.SetActive(false);
        emptyDetailsText.gameObject.SetActive(true);
        emptyListText.gameObject.SetActive(true);

        QuestPageView questLogView = GetOrAdd<QuestPageView>(page);
        SerializedObject serializedView = new(questLogView);
        serializedView.FindProperty("hierarchyVersion").intValue = BuilderVersion;
        serializedView.FindProperty("quests").arraySize = 0;
        serializedView.FindProperty("listContent").objectReferenceValue = listContent;
        serializedView.FindProperty("listItemTemplate").objectReferenceValue = itemTemplate;
        serializedView.FindProperty("listScroll").objectReferenceValue = listScroll;
        serializedView.FindProperty("titleText").objectReferenceValue = titleText;
        serializedView.FindProperty("giverText").objectReferenceValue = giverText;
        serializedView.FindProperty("descriptionText").objectReferenceValue = descriptionText;
        serializedView.FindProperty("detailsContentRoot").objectReferenceValue = detailsContent.gameObject;
        serializedView.FindProperty("rewardRoot").objectReferenceValue = rewardRoot;
        serializedView.FindProperty("rewardLabelText").objectReferenceValue = rewardLabel;
        serializedView.FindProperty("rewardIcon").objectReferenceValue = rewardIcon;
        serializedView.FindProperty("detailsScroll").objectReferenceValue = detailsScroll;
        serializedView.FindProperty("emptyDetailsText").objectReferenceValue = emptyDetailsText;
        serializedView.FindProperty("emptyListText").objectReferenceValue = emptyListText;
        serializedView.ApplyModifiedPropertiesWithoutUndo();

        AssignQuestPage(window, page);
        SetLayerRecursive(page, LayerMask.NameToLayer("UI"));
    }

    private static QuestListItemView CreateQuestItemTemplate(Transform parent, TMP_Text fontSource)
    {
        GameObject item = CreatePanel(parent, "QuestListItemTemplate", QuestListItemColor, false);
        GetOrAdd<RectTransform>(item).sizeDelta = new Vector2(0f, 86f);
        AddLayoutElement(item, -1f, 86f, 1f, 0f);

        Image background = GetOrAdd<Image>(item);
        background.raycastTarget = true;

        Button button = GetOrAdd<Button>(item);
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        button.targetGraphic = background;

        HorizontalLayoutGroup itemLayout = GetOrAdd<HorizontalLayoutGroup>(item);
        itemLayout.padding = new RectOffset(15, 10, 8, 8);
        itemLayout.spacing = 10;
        itemLayout.childControlWidth = true;
        itemLayout.childControlHeight = true;
        itemLayout.childForceExpandWidth = false;
        itemLayout.childForceExpandHeight = true;

        TMP_Text title = CreateText(item.transform, "TitleText", "Quest", 24f, TextAlignmentOptions.MidlineLeft, fontSource, TextColor);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        AddLayoutElement(title.gameObject, -1f, -1f, 1f, 1f);

        Image rewardIcon = CreateImage(item.transform, "RewardIcon", Color.white);
        rewardIcon.preserveAspect = true;
        rewardIcon.enabled = false;
        AddLayoutElement(rewardIcon.gameObject, 56f, 56f, 0f, 0f);

        QuestListItemView itemView = GetOrAdd<QuestListItemView>(item);
        SerializedObject serializedItem = new(itemView);
        serializedItem.FindProperty("button").objectReferenceValue = button;
        serializedItem.FindProperty("backgroundImage").objectReferenceValue = background;
        serializedItem.FindProperty("titleText").objectReferenceValue = title;
        serializedItem.FindProperty("rewardIcon").objectReferenceValue = rewardIcon;
        serializedItem.FindProperty("normalColor").colorValue = QuestListItemColor;
        serializedItem.FindProperty("selectedColor").colorValue = SelectedColor;
        serializedItem.FindProperty("normalTextColor").colorValue = TextColor;
        serializedItem.FindProperty("selectedTextColor").colorValue = Color.white;
        serializedItem.ApplyModifiedPropertiesWithoutUndo();

        return itemView;
    }

    private static void CreatePreviewQuestItems(Transform listContent, QuestListItemView template, IReadOnlyList<QuestData> quests)
    {
        if (listContent == null || template == null)
            return;

        for (int i = 0; i < quests.Count; i++)
        {
            QuestListItemView item = Object.Instantiate(template, listContent);
            item.name = $"QuestListItem_{i + 1}_{quests[i].Title}";
            item.gameObject.SetActive(true);
            item.Setup(quests[i], i, false, null);
        }
    }

    private static TMP_Text CreateEmptyDetailsText(ScrollRect detailsScroll, TMP_Text fontSource)
    {
        Transform parent = detailsScroll.viewport != null ? detailsScroll.viewport : detailsScroll.transform;
        TMP_Text emptyText = CreateText(parent, "EmptyDetailsText", "Select a quest to view details", 30f, TextAlignmentOptions.Center, fontSource, TextColor);
        RectTransform rect = GetOrAdd<RectTransform>(emptyText.gameObject);
        Stretch(rect, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));
        emptyText.textWrappingMode = TextWrappingModes.Normal;
        emptyText.raycastTarget = false;
        emptyText.transform.SetAsLastSibling();
        return emptyText;
    }

    private static TMP_Text CreateEmptyListText(ScrollRect listScroll, TMP_Text fontSource)
    {
        Transform parent = listScroll.viewport != null ? listScroll.viewport : listScroll.transform;
        TMP_Text emptyText = CreateText(parent, "EmptyQuestListText", "No quests yet", 30f, TextAlignmentOptions.Center, fontSource, TextColor);
        RectTransform rect = GetOrAdd<RectTransform>(emptyText.gameObject);
        Stretch(rect, Vector2.zero, Vector2.one, new Vector2(20f, 20f), new Vector2(-20f, -20f));
        emptyText.textWrappingMode = TextWrappingModes.Normal;
        emptyText.raycastTarget = false;
        emptyText.transform.SetAsLastSibling();
        return emptyText;
    }

    private static void SetQuestDetailsPreview(
        QuestData quest,
        TMP_Text titleText,
        TMP_Text giverText,
        TMP_Text descriptionText,
        GameObject rewardRoot,
        TMP_Text rewardLabel,
        Image rewardIcon)
    {
        if (quest == null)
        {
            if (titleText != null)
                titleText.text = string.Empty;

            if (descriptionText != null)
                descriptionText.text = string.Empty;

            if (giverText != null)
                giverText.text = string.Empty;

            if (rewardRoot != null)
                rewardRoot.SetActive(false);

            return;
        }

        if (titleText != null)
            titleText.text = quest.Title;

        if (giverText != null)
            giverText.text = string.IsNullOrWhiteSpace(quest.GiverName) ? string.Empty : $"Giver: {quest.GiverName}";

        if (descriptionText != null)
            descriptionText.text = quest.Description;

        bool hasReward = quest.RewardIcon != null || !string.IsNullOrWhiteSpace(quest.RewardLabel);
        if (rewardRoot != null)
            rewardRoot.SetActive(hasReward);

        if (rewardLabel != null)
            rewardLabel.text = quest.RewardLabel;

        if (rewardIcon != null)
        {
            rewardIcon.sprite = quest.RewardIcon;
            rewardIcon.enabled = quest.RewardIcon != null;
        }
    }

    private static ScrollRect CreateDetailsScroll(Transform parent, out RectTransform content)
    {
        GameObject root = CreatePanel(parent, "QuestDetailsScroll", ScrollPanelColor, false);
        AddLayoutElement(root, -1f, -1f, 1f, 1f);

        GameObject viewport = GetOrCreateChild(root.transform, "Viewport");
        RectTransform viewportRect = GetOrAdd<RectTransform>(viewport);
        Stretch(viewportRect, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, 0f));
        Image viewportImage = GetOrAdd<Image>(viewport);
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;
        GetOrAdd<Mask>(viewport).showMaskGraphic = false;

        GameObject contentObject = GetOrCreateChild(viewport.transform, "QuestDetailsContent");
        content = GetOrAdd<RectTransform>(contentObject);
        Stretch(content, new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1f);

        HorizontalLayoutGroup contentLayout = GetOrAdd<HorizontalLayoutGroup>(contentObject);
        contentLayout.padding = new RectOffset(12, 12, 12, 12);
        contentLayout.spacing = 12;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = true;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(contentObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Scrollbar scrollbar = CreateScrollbar(root.transform);

        ScrollRect scroll = GetOrAdd<ScrollRect>(root);
        scroll.content = content;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 12f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scroll.verticalScrollbarSpacing = -20f;
        return scroll;
    }

    private static TMP_Text CreateHeader(Transform parent, string name, string title, TMP_Text fontSource)
    {
        GameObject header = CreatePanel(parent, name, HeaderColor, false);
        AddLayoutElement(header, -1f, 56f, -1f, 0f);

        TMP_Text titleText = CreateText(header.transform, "TitleText", title, 30f, TextAlignmentOptions.MidlineLeft, fontSource, Color.white);
        RectTransform titleRect = GetOrAdd<RectTransform>(titleText.gameObject);
        Stretch(titleRect, Vector2.zero, Vector2.one, new Vector2(15f, 0f), new Vector2(-15f, 0f));
        return titleText;
    }

    private static ScrollRect CreateScroll(Transform parent, string name, string contentName, Color color, float preferredHeight, out RectTransform content)
    {
        GameObject root = CreatePanel(parent, name, color, false);
        AddLayoutElement(root, -1f, preferredHeight, 1f, preferredHeight < 0f ? 1f : 0f);

        GameObject viewport = GetOrCreateChild(root.transform, "Viewport");
        RectTransform viewportRect = GetOrAdd<RectTransform>(viewport);
        Stretch(viewportRect, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, 0f));
        Image viewportImage = GetOrAdd<Image>(viewport);
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;
        GetOrAdd<Mask>(viewport).showMaskGraphic = false;

        GameObject contentObject = GetOrCreateChild(viewport.transform, contentName);
        content = GetOrAdd<RectTransform>(contentObject);
        Stretch(content, new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup contentLayout = GetOrAdd<VerticalLayoutGroup>(contentObject);
        contentLayout.spacing = 5;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(contentObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Scrollbar scrollbar = CreateScrollbar(root.transform);

        ScrollRect scroll = GetOrAdd<ScrollRect>(root);
        scroll.content = content;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 12f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scroll.verticalScrollbarSpacing = -20f;
        return scroll;
    }

    private static Scrollbar CreateScrollbar(Transform parent)
    {
        GameObject scrollbarObject = GetOrCreateChild(parent, "Scrollbar Vertical");
        RectTransform scrollbarRect = GetOrAdd<RectTransform>(scrollbarObject);
        Stretch(scrollbarRect, new Vector2(1f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
        scrollbarRect.sizeDelta = new Vector2(20f, 0f);
        scrollbarRect.pivot = new Vector2(1f, 1f);

        Image scrollbarImage = GetOrAdd<Image>(scrollbarObject);
        scrollbarImage.color = ScrollbarTrackColor;

        GameObject slidingArea = GetOrCreateChild(scrollbarObject.transform, "Sliding Area");
        RectTransform slidingRect = GetOrAdd<RectTransform>(slidingArea);
        Stretch(slidingRect, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));

        GameObject handle = GetOrCreateChild(slidingArea.transform, "Handle");
        RectTransform handleRect = GetOrAdd<RectTransform>(handle);
        Stretch(handleRect, Vector2.zero, Vector2.one, new Vector2(-10f, -10f), new Vector2(10f, 10f));
        Image handleImage = GetOrAdd<Image>(handle);
        handleImage.color = ScrollbarHandleColor;

        Scrollbar scrollbar = GetOrAdd<Scrollbar>(scrollbarObject);
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImage;
        scrollbar.handleRect = handleRect;
        scrollbar.value = 1f;
        return scrollbar;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color, bool withBorder)
    {
        GameObject panel = GetOrCreateChild(parent, name);
        Image image = GetOrAdd<Image>(panel);
        image.color = color;
        image.raycastTarget = true;

        if (withBorder)
        {
            Outline outline = GetOrAdd<Outline>(panel);
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
        }
        else if (panel.TryGetComponent(out Outline outline))
        {
            Object.DestroyImmediate(outline, true);
        }

        return panel;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float fontSize, TextAlignmentOptions alignment, TMP_Text fontSource, Color color)
    {
        GameObject textObject = GetOrCreateChild(parent, name);
        TMP_Text text = GetOrAdd<TextMeshProUGUI>(textObject);
        if (fontSource != null)
        {
            text.font = fontSource.font;
            text.fontMaterial = fontSource.fontMaterial;
        }

        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject imageObject = GetOrCreateChild(parent, name);
        Image image = GetOrAdd<Image>(imageObject);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void AssignQuestPage(InventoryWindow window, GameObject page)
    {
        SerializedObject serializedWindow = new(window);
        SerializedProperty pages = serializedWindow.FindProperty("pages");
        if (pages.arraySize <= QuestPageIndex)
            pages.arraySize = QuestPageIndex + 1;

        pages.GetArrayElementAtIndex(QuestPageIndex).objectReferenceValue = page;
        serializedWindow.ApplyModifiedPropertiesWithoutUndo();
    }

    private static List<QuestData> LoadSampleQuests()
    {
        List<QuestData> sampleQuests = new();
        foreach (string path in SampleQuestPaths)
        {
            QuestData quest = AssetDatabase.LoadAssetAtPath<QuestData>(path);
            if (quest != null)
                sampleQuests.Add(quest);
        }

        return sampleQuests;
    }

    private static void AssignSampleQuests(SerializedProperty questsProperty, IReadOnlyList<QuestData> sampleQuests)
    {
        questsProperty.arraySize = sampleQuests.Count;
        for (int i = 0; i < sampleQuests.Count; i++)
            questsProperty.GetArrayElementAtIndex(i).objectReferenceValue = sampleQuests[i];
    }

    private static bool IsQuestPageAssigned(InventoryWindow window, GameObject page)
    {
        SerializedObject serializedWindow = new(window);
        SerializedProperty pages = serializedWindow.FindProperty("pages");
        return pages.arraySize > QuestPageIndex
            && pages.GetArrayElementAtIndex(QuestPageIndex).objectReferenceValue == page;
    }

    private static bool IsCurrent(QuestPageView view)
    {
        if (view == null)
            return false;

        SerializedObject serializedView = new(view);
        return serializedView.FindProperty("hierarchyVersion").intValue == BuilderVersion;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void AddLayoutElement(GameObject target, float preferredWidth, float preferredHeight, float flexibleWidth, float flexibleHeight)
    {
        LayoutElement layoutElement = GetOrAdd<LayoutElement>(target);
        layoutElement.minWidth = -1f;
        layoutElement.minHeight = -1f;
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.preferredHeight = preferredHeight;
        layoutElement.flexibleWidth = flexibleWidth;
        layoutElement.flexibleHeight = flexibleHeight;
    }

    private static void SetMinLayoutElement(GameObject target, float minWidth, float minHeight)
    {
        LayoutElement layoutElement = GetOrAdd<LayoutElement>(target);
        layoutElement.minWidth = minWidth;
        layoutElement.minHeight = minHeight;
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            existing.gameObject.layer = parent.gameObject.layer;
            return existing.gameObject;
        }

        GameObject child = new(name, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        return child;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        if (gameObject.TryGetComponent(out T component))
            return component;

        return gameObject.AddComponent<T>();
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(parent.GetChild(i).gameObject, true);
    }

    private static void SetLayerRecursive(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
