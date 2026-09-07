using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DialogueRootHierarchyBuilder
{
    private const string ExplorationScenePath = "Assets/_Project/Scenes/ExplorationScene.unity";
    private const string CanvasName = "ExplorationCanvas";
    private const string AutoCreateSessionKey = "InsideTheScreen.DialogueRootHierarchyBuilder.AutoCreate";
    private const string AutoCreateVersionKey = "InsideTheScreen.DialogueRootHierarchyBuilder.AutoCreateVersion";
    private const int BuilderVersion = 3;

    [InitializeOnLoadMethod]
    private static void QueueAutoCreate()
    {
        EditorApplication.delayCall -= AutoCreateInOpenScene;
        EditorApplication.delayCall += AutoCreateInOpenScene;
    }

    private static void AutoCreateInOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Canvas canvas = FindCanvas(SceneManager.GetActiveScene());
        if (canvas == null)
            return;

        bool alreadyUpdated = SessionState.GetBool(AutoCreateSessionKey, false)
            && SessionState.GetInt(AutoCreateVersionKey, 0) == BuilderVersion;

        if (alreadyUpdated && canvas.transform.Find("DialogueRoot") != null && Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include) != null)
            return;

        SessionState.SetBool(AutoCreateSessionKey, true);
        SessionState.SetInt(AutoCreateVersionKey, BuilderVersion);
        Build(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("DialogueRoot hierarchy was auto-created in the active scene.");
    }

    public static void CreateInActiveScene()
    {
        Canvas canvas = FindCanvas(SceneManager.GetActiveScene());
        if (canvas == null)
        {
            Debug.LogError($"{CanvasName} was not found in the active scene.");
            return;
        }

        Build(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("DialogueRoot hierarchy was created/updated in the active scene.");
    }

    public static void CreateInExplorationScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.OpenScene(ExplorationScenePath, OpenSceneMode.Single);
        Canvas canvas = FindCanvas(scene);
        if (canvas == null)
        {
            Debug.LogError($"{CanvasName} was not found in {ExplorationScenePath}.");
            return;
        }

        Build(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("DialogueRoot hierarchy was created/updated and ExplorationScene was saved.");
    }

    private static void Build(Canvas canvas)
    {
        GameObject root = GetOrCreateChild(canvas.transform, "DialogueRoot");
        root.layer = LayerMask.NameToLayer("UI");
        RectTransform rootRect = GetOrAdd<RectTransform>(root);
        Stretch(rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        rootRect.SetAsLastSibling();

        CanvasGroup canvasGroup = GetOrAdd<CanvasGroup>(root);
        DialogueView view = GetOrAdd<DialogueView>(root);

        GameObject dimBackground = GetOrCreateChild(root.transform, "DimBackground");
        RectTransform dimRect = GetOrAdd<RectTransform>(dimBackground);
        Stretch(dimRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dimImage = GetOrAdd<Image>(dimBackground);
        dimImage.color = new Color(0f, 0f, 0f, 0.38f);
        dimImage.raycastTarget = false;
        dimRect.SetAsFirstSibling();

        GameObject stage = GetOrCreateChild(root.transform, "SpeakerStage");
        RectTransform stageRect = GetOrAdd<RectTransform>(stage);
        Stretch(stageRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject leftRoot = CreateSpeakerRoot(
            stage.transform,
            "LeftSpeakerRoot",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(110f, 0f),
            new Vector2(610f, -70f),
            new Vector2(0f, 0f));
        Image leftPortrait = CreatePortrait(leftRoot.transform, "LeftPortrait");

        GameObject rightRoot = CreateSpeakerRoot(
            stage.transform,
            "RightSpeakerRoot",
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(-610f, 0f),
            new Vector2(-110f, -70f),
            new Vector2(1f, 0f));
        Image rightPortrait = CreatePortrait(rightRoot.transform, "RightPortrait");

        GameObject panel = GetOrCreateChild(root.transform, "DialoguePanel");
        RectTransform panelRect = GetOrAdd<RectTransform>(panel);
        Stretch(panelRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(32f, 24f), new Vector2(-32f, 324f));
        Image panelImage = GetOrAdd<Image>(panel);
        panelImage.color = new Color(0.12f, 0.09f, 0.08f, 0.94f);
        panelImage.raycastTarget = true;
        Outline panelOutline = GetOrAdd<Outline>(panel);
        panelOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        panelOutline.effectDistance = new Vector2(0f, 2f);
        panelRect.SetAsLastSibling();

        GameObject topLine = GetOrCreateChild(panel.transform, "TopLine");
        RectTransform topLineRect = GetOrAdd<RectTransform>(topLine);
        Stretch(topLineRect, new Vector2(0f, 1f), Vector2.one, Vector2.zero, new Vector2(0f, 4f));
        Image topLineImage = GetOrAdd<Image>(topLine);
        topLineImage.color = new Color(0.73f, 0.56f, 0.42f, 1f);
        topLineImage.raycastTarget = false;

        GameObject namePlate = GetOrCreateChild(panel.transform, "NamePlate");
        RectTransform namePlateRect = GetOrAdd<RectTransform>(namePlate);
        Anchor(namePlateRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(72f, -36f), new Vector2(360f, 72f), new Vector2(0f, 0.5f));
        Image namePlateImage = GetOrAdd<Image>(namePlate);
        namePlateImage.color = new Color(0.24f, 0.16f, 0.12f, 1f);
        Outline nameOutline = GetOrAdd<Outline>(namePlate);
        nameOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        nameOutline.effectDistance = new Vector2(1f, -1f);

        TMP_Text speakerName = CreateText(namePlate.transform, "SpeakerNameText", 42f, TextAlignmentOptions.Center);
        Stretch(speakerName.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        speakerName.color = new Color(0.94f, 0.88f, 0.80f, 1f);
        speakerName.text = "Haru";

        TMP_Text dialogueText = CreateText(panel.transform, "DialogueText", 32f, TextAlignmentOptions.TopLeft);
        Stretch(dialogueText.rectTransform, Vector2.zero, Vector2.one, new Vector2(48f, 34f), new Vector2(-170f, -104f));
        dialogueText.color = new Color(0.94f, 0.88f, 0.80f, 1f);
        dialogueText.text = string.Empty;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;

        GameObject continueButtonObject = GetOrCreateChild(panel.transform, "ContinueButton");
        RectTransform continueRect = GetOrAdd<RectTransform>(continueButtonObject);
        Anchor(continueRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-46f, 42f), new Vector2(76f, 58f), new Vector2(1f, 0.5f));
        Image continueImage = GetOrAdd<Image>(continueButtonObject);
        continueImage.color = new Color(0.24f, 0.16f, 0.12f, 1f);
        Button continueButton = GetOrAdd<Button>(continueButtonObject);

        TMP_Text continueIndicator = CreateText(continueButtonObject.transform, "ContinueIndicator", 40f, TextAlignmentOptions.Center);
        Stretch(continueIndicator.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        continueIndicator.color = new Color(0.94f, 0.88f, 0.80f, 1f);
        continueIndicator.text = ">";

        AssignDialogueView(view, root, canvasGroup, leftRoot, leftPortrait, rightRoot, rightPortrait, namePlateRect, speakerName, dialogueText, continueButton, continueIndicator.gameObject);
        AssignDialogueManager(view);
        root.SetActive(false);
    }

    private static GameObject CreateSpeakerRoot(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Vector2 pivot)
    {
        GameObject root = GetOrCreateChild(parent, name);
        RectTransform rect = GetOrAdd<RectTransform>(root);
        Stretch(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return root;
    }

    private static Image CreatePortrait(Transform parent, string name)
    {
        GameObject portrait = GetOrCreateChild(parent, name);
        RectTransform rect = GetOrAdd<RectTransform>(portrait);
        Stretch(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image image = GetOrAdd<Image>(portrait);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        image.enabled = image.sprite != null;
        return image;
    }

    private static TMP_Text CreateText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = GetOrCreateChild(parent, name);
        TMP_Text text = GetOrAdd<TextMeshProUGUI>(textObject);
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static void AssignDialogueView(
        DialogueView view,
        GameObject root,
        CanvasGroup canvasGroup,
        GameObject leftSpeakerRoot,
        Image leftPortrait,
        GameObject rightSpeakerRoot,
        Image rightPortrait,
        RectTransform speakerNamePlate,
        TMP_Text speakerNameText,
        TMP_Text dialogueText,
        Button continueButton,
        GameObject continueIndicator)
    {
        SerializedObject serializedView = new SerializedObject(view);
        serializedView.FindProperty("root").objectReferenceValue = root;
        serializedView.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
        serializedView.FindProperty("leftSpeakerRoot").objectReferenceValue = leftSpeakerRoot;
        serializedView.FindProperty("leftPortrait").objectReferenceValue = leftPortrait;
        serializedView.FindProperty("rightSpeakerRoot").objectReferenceValue = rightSpeakerRoot;
        serializedView.FindProperty("rightPortrait").objectReferenceValue = rightPortrait;
        serializedView.FindProperty("speakerNamePlate").objectReferenceValue = speakerNamePlate;
        serializedView.FindProperty("speakerNameText").objectReferenceValue = speakerNameText;
        serializedView.FindProperty("dialogueText").objectReferenceValue = dialogueText;
        serializedView.FindProperty("continueButton").objectReferenceValue = continueButton;
        serializedView.FindProperty("continueIndicator").objectReferenceValue = continueIndicator;
        serializedView.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignDialogueManager(DialogueView view)
    {
        DialogueManager manager = Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        if (manager == null)
            manager = new GameObject("DialogueManager").AddComponent<DialogueManager>();

        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("view").objectReferenceValue = view;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Canvas FindCanvas(Scene scene)
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].gameObject.scene == scene && canvases[i].name == CanvasName)
                return canvases[i];
        }

        return null;
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject child = new GameObject(name, typeof(RectTransform));
        child.layer = LayerMask.NameToLayer("UI");
        child.transform.SetParent(parent, false);
        return child;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        if (gameObject.TryGetComponent(out T component))
            return component;

        return gameObject.AddComponent<T>();
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
    }

    private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        Anchor(rect, anchorMin, anchorMax, position, size, new Vector2(0.5f, 0.5f));
    }

    private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
}
