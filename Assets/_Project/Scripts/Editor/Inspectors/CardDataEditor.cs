using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardData))]
public class CardDataEditor : UnityEditor.Editor
{
    private SerializedProperty _cardIdProp;
    private SerializedProperty _cardNameProp;
    private SerializedProperty _descriptionProp;
    private SerializedProperty _shortDescriptionProp;
    private SerializedProperty _iconProp;
    private SerializedProperty _categoriesProp;
    private SerializedProperty _costProp;
    private SerializedProperty _conditionsProp;
    private SerializedProperty _effectsProp;
    private SerializedProperty _statusEffectsProp;

    private void OnEnable()
    {
        _cardIdProp = serializedObject.FindProperty("cardID");
        _cardNameProp = serializedObject.FindProperty("cardName");
        _descriptionProp = serializedObject.FindProperty("description");
        _shortDescriptionProp = serializedObject.FindProperty("shortDescription");
        _iconProp = serializedObject.FindProperty("icon");
        _categoriesProp = serializedObject.FindProperty("categories");
        _costProp = serializedObject.FindProperty("cost");
        _conditionsProp = serializedObject.FindProperty("conditions");
        _effectsProp = serializedObject.FindProperty("instantEffects");
        _statusEffectsProp = serializedObject.FindProperty("statusApplications");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorSectionGUI.DrawSection("Card Identity", DrawIdentitySection, 0f);
        EditorSectionGUI.DrawSection("Gameplay", DrawGameplaySection);
        EditorSectionGUI.DrawSection("Validation", DrawValidation);
        DrawQuickActions();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentitySection()
    {
        EditorGUILayout.PropertyField(_cardIdProp);
        EditorGUILayout.PropertyField(_cardNameProp);
        EditorGUILayout.PropertyField(_descriptionProp);
        EditorGUILayout.PropertyField(_shortDescriptionProp);
        DrawDescriptionTemplateHelp();
        DrawFormattedPreview();
        EditorGUILayout.PropertyField(_iconProp);
        EditorGUILayout.PropertyField(_categoriesProp, true);
    }

    private void DrawGameplaySection()
    {
        EditorGUILayout.PropertyField(_costProp);
        EditorGUILayout.PropertyField(_conditionsProp, true);
        EditorGUILayout.PropertyField(_effectsProp, true);
        EditorGUILayout.PropertyField(_statusEffectsProp, true);
    }

    public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
    {
        CardData card = target as CardData;
        if (card == null || card.Icon == null)
            return base.RenderStaticPreview(assetPath, subAssets, width, height);

        return CreateSpritePreview(card.Icon, width, height);
    }

    private void DrawValidation()
    {
        if (_costProp.intValue < 0)
            EditorGUILayout.HelpBox("Cost is negative. Card costs are expected to be >= 0.", MessageType.Warning);

        if (string.IsNullOrWhiteSpace(_cardIdProp.stringValue))
            EditorGUILayout.HelpBox("Card ID is empty. Use stable unique IDs for save/load and content references.", MessageType.Warning);

        if (string.IsNullOrWhiteSpace(_cardNameProp.stringValue))
            EditorGUILayout.HelpBox("Card Name is empty. UI will fall back to asset name.", MessageType.Info);

        if (_effectsProp.arraySize == 0 && _statusEffectsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "Card has no instant effects or status applications. CardResolver will not apply anything when this card is played.",
                MessageType.Warning);
            return;
        }

        int missingEffectCount = 0;
        int missingStatusCount = 0;
        int missingTargetingCount = 0;
        int invalidApplyChanceCount = 0;
        int invalidDurationCount = 0;
        int invalidStackCount = 0;
        int emptyConditionCount = 0;

        for (int i = 0; i < _effectsProp.arraySize; i++)
        {
            SerializedProperty entryProp = _effectsProp.GetArrayElementAtIndex(i);
            SerializedProperty effectProp = entryProp.FindPropertyRelative("effect");
            SerializedProperty targetingProp = entryProp.FindPropertyRelative("targeting");
            SerializedProperty applyChanceProp = entryProp.FindPropertyRelative("applyChance");
            SerializedProperty conditionsProp = entryProp.FindPropertyRelative("conditions");

            if (effectProp.objectReferenceValue == null)
                missingEffectCount++;
            if (targetingProp.objectReferenceValue == null)
                missingTargetingCount++;
            if (applyChanceProp.floatValue < 0f || applyChanceProp.floatValue > 1f)
                invalidApplyChanceCount++;

            emptyConditionCount += BattleEffectSpecEditorUtility.CountEmptyConditions(conditionsProp);
        }

        for (int i = 0; i < _statusEffectsProp.arraySize; i++)
        {
            SerializedProperty entryProp = _statusEffectsProp.GetArrayElementAtIndex(i);
            SerializedProperty statusProp = entryProp.FindPropertyRelative("status");
            SerializedProperty targetingProp = entryProp.FindPropertyRelative("targeting");
            SerializedProperty durationProp = entryProp.FindPropertyRelative("duration");
            SerializedProperty stacksProp = entryProp.FindPropertyRelative("stacks");
            SerializedProperty applyChanceProp = entryProp.FindPropertyRelative("applyChance");
            SerializedProperty conditionsProp = entryProp.FindPropertyRelative("conditions");

            if (statusProp.objectReferenceValue == null)
                missingStatusCount++;
            if (targetingProp.objectReferenceValue == null)
                missingTargetingCount++;
            if (applyChanceProp.floatValue < 0f || applyChanceProp.floatValue > 1f)
                invalidApplyChanceCount++;
            if (durationProp.intValue < 0)
                invalidDurationCount++;
            if (stacksProp.intValue < 1)
                invalidStackCount++;

            emptyConditionCount += BattleEffectSpecEditorUtility.CountEmptyConditions(conditionsProp);
        }

        if (missingEffectCount > 0)
            EditorGUILayout.HelpBox($"Instant Effects without InstantBattleEffect: {missingEffectCount}. They are skipped at runtime.", MessageType.Warning);
        if (missingStatusCount > 0)
            EditorGUILayout.HelpBox($"Status applications without BattleStatus: {missingStatusCount}. They are skipped at runtime.", MessageType.Warning);
        if (missingTargetingCount > 0)
            EditorGUILayout.HelpBox($"Entries without targeting profile: {missingTargetingCount}. They are skipped by CardResolver.", MessageType.Warning);
        if (invalidApplyChanceCount > 0)
            EditorGUILayout.HelpBox($"Entries with invalid Apply Chance (expected 0..1): {invalidApplyChanceCount}.", MessageType.Warning);
        if (emptyConditionCount > 0)
            EditorGUILayout.HelpBox($"Empty effect conditions: {emptyConditionCount}. Assign a BattleCondition asset or remove the condition entry.", MessageType.Warning);
        if (invalidDurationCount > 0)
            EditorGUILayout.HelpBox($"Status applications with invalid duration (expected >= 0): {invalidDurationCount}.", MessageType.Warning);
        if (invalidStackCount > 0)
            EditorGUILayout.HelpBox($"Status applications with invalid stacks (expected >= 1): {invalidStackCount}.", MessageType.Warning);
    }

    private void DrawDescriptionTemplateHelp()
    {
        EditorSectionGUI.DrawTemplateHelp(
            "Description supports: {value}, {total}, {duration}, {stacks}, {chance}, {effect}, {status}. " +
            "For exact entries use: {instant0.value}, {instant0.total}, {instant0.effect}, {status0.value}, {status0.total}, {status0.status}, {status0.duration}.");
    }

    private void DrawFormattedPreview()
    {
        CardData card = target as CardData;
        if (card == null)
            return;

        string preview = card.Description;
        string shortPreview = card.ShortDescription;

        if (!string.IsNullOrWhiteSpace(preview))
            EditorSectionGUI.DrawReadonlyPreview("Formatted Description", preview, 34f);

        if (!string.IsNullOrWhiteSpace(shortPreview))
            EditorSectionGUI.DrawReadonlyPreview("Card Text Preview", shortPreview, 34f);
    }

    private void DrawQuickActions()
    {
        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto-fill ID From Name"))
            {
                string source = string.IsNullOrWhiteSpace(_cardNameProp.stringValue)
                    ? target.name
                    : _cardNameProp.stringValue;
                _cardIdProp.stringValue = BuildId(source);
            }

            if (GUILayout.Button("Set Name From Asset"))
                _cardNameProp.stringValue = target.name;
        }
    }

    private static string BuildId(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "new_card";

        string lowered = input.Trim().ToLowerInvariant();
        var chars = lowered.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        }

        string result = new string(chars);
        while (result.Contains("__"))
            result = result.Replace("__", "_");

        return result.Trim('_');
    }

    private static Texture2D CreateSpritePreview(Sprite sprite, int width, int height)
    {
        Texture sourceTexture = sprite.texture;
        Rect textureRect = sprite.textureRect;

        Texture2D preview = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;

        RenderTexture.active = renderTexture;
        GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
        GL.PushMatrix();
        GL.LoadPixelMatrix(0f, width, height, 0f);

        float scale = Mathf.Min(width / textureRect.width, height / textureRect.height);
        float drawWidth = textureRect.width * scale;
        float drawHeight = textureRect.height * scale;
        Rect drawRect = new Rect(
            (width - drawWidth) * 0.5f,
            (height - drawHeight) * 0.5f,
            drawWidth,
            drawHeight);

        Rect sourceRect = new Rect(
            textureRect.x / sourceTexture.width,
            textureRect.y / sourceTexture.height,
            textureRect.width / sourceTexture.width,
            textureRect.height / sourceTexture.height);

        Graphics.DrawTexture(drawRect, sourceTexture, sourceRect, 0, 0, 0, 0);
        GL.PopMatrix();

        preview.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        preview.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);

        return preview;
    }
}
