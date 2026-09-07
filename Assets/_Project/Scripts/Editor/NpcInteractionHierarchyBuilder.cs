using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NpcInteractionHierarchyBuilder
{
    private const string AutoSetupSessionKey = "InsideTheScreen.NpcInteractionHierarchyBuilder.AutoSetup";
    private const string AutoSetupVersionKey = "InsideTheScreen.NpcInteractionHierarchyBuilder.AutoSetupVersion";
    private const int BuilderVersion = 6;

    [InitializeOnLoadMethod]
    private static void QueueAutoSetup()
    {
        EditorApplication.delayCall -= AutoSetupOpenScene;
        EditorApplication.delayCall += AutoSetupOpenScene;
    }

    private static void AutoSetupOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        bool alreadyUpdated = SessionState.GetBool(AutoSetupSessionKey, false)
            && SessionState.GetInt(AutoSetupVersionKey, 0) == BuilderVersion;

        if (alreadyUpdated)
            return;

        NpcCharacter[] npcs = Object.FindObjectsByType<NpcCharacter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (npcs.Length == 0)
            return;

        bool changed = false;
        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i] == null || !npcs[i].gameObject.scene.IsValid())
                continue;

            Setup(npcs[i]);
            changed = true;
        }

        if (!changed)
            return;

        SessionState.SetBool(AutoSetupSessionKey, true);
        SessionState.SetInt(AutoSetupVersionKey, BuilderVersion);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("NPC interaction prompt hierarchy was auto-created/updated.");
    }

    private static void Setup(NpcCharacter npc)
    {
        CleanupGeneratedOverheadObjects(npc);
        NpcInteraction interaction = GetOrAdd<NpcInteraction>(npc.gameObject);

        GameObject promptRoot = CreateWorldCanvas(npc.transform, "PromptCanvas", new Vector3(0f, 1.55f, 0f), new Vector2(420f, 96f));
        CanvasGroup promptCanvasGroup = promptRoot.GetComponent<CanvasGroup>();

        TMP_Text promptText = CreateText(promptRoot.transform, "PromptText", 28f, new Color(1f, 0.95f, 0.82f, 1f));
        promptText.text = "E - Talk";

        if (interaction.TryGetComponent(out Collider2D collider))
            collider.isTrigger = true;
        else
            interaction.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;

        SerializedObject serializedInteraction = new SerializedObject(interaction);
        serializedInteraction.FindProperty("npc").objectReferenceValue = npc;
        serializedInteraction.FindProperty("promptRoot").objectReferenceValue = promptRoot;
        serializedInteraction.FindProperty("promptCanvasGroup").objectReferenceValue = promptCanvasGroup;
        serializedInteraction.FindProperty("promptText").objectReferenceValue = promptText;
        serializedInteraction.FindProperty("interactHintTemplate").stringValue = "{0} - {1}";
        serializedInteraction.FindProperty("overheadLineDuration").floatValue = 2f;
        serializedInteraction.FindProperty("promptReturnDelay").floatValue = 0.45f;
        serializedInteraction.ApplyModifiedPropertiesWithoutUndo();

        promptRoot.SetActive(false);
    }

    private static void CleanupGeneratedOverheadObjects(NpcCharacter npc)
    {
        Transform overheadCanvas = npc.transform.Find("OverheadCanvas");
        if (overheadCanvas != null)
            Object.DestroyImmediate(overheadCanvas.gameObject, true);

        Transform promptCanvas = npc.transform.Find("PromptCanvas");
        Transform overheadText = promptCanvas != null ? promptCanvas.Find("OverheadText") : null;
        if (overheadText != null)
            Object.DestroyImmediate(overheadText.gameObject, true);
    }

    private static GameObject CreateWorldCanvas(Transform parent, string name, Vector3 localPosition, Vector2 size)
    {
        GameObject root = GetOrCreateChild(parent, name);
        root.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = GetOrAdd<RectTransform>(root);
        rect.localPosition = localPosition;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * 0.01f;
        rect.sizeDelta = size;
        rect.pivot = new Vector2(0.5f, 0.5f);

        Canvas canvas = GetOrAdd<Canvas>(root);
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
        scaler.dynamicPixelsPerUnit = 10f;

        CanvasGroup canvasGroup = GetOrAdd<CanvasGroup>(root);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        return root;
    }

    private static TMP_Text CreateText(Transform parent, string name, float fontSize, Color color)
    {
        GameObject textObject = GetOrCreateChild(parent, name);
        textObject.layer = LayerMask.NameToLayer("UI");

        RectTransform rect = GetOrAdd<RectTransform>(textObject);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        TMP_Text text = GetOrAdd<TextMeshProUGUI>(textObject);
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.gameObject;

        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        if (gameObject.TryGetComponent(out T component))
            return component;

        return gameObject.AddComponent<T>();
    }
}
