using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardData))]
public class CardDataEditor : UnityEditor.Editor
{
    private SerializedProperty _cardIdProp;
    private SerializedProperty _cardNameProp;
    private SerializedProperty _descriptionProp;
    private SerializedProperty _iconProp;
    private SerializedProperty _costProp;
    private SerializedProperty _conditionsProp;
    private SerializedProperty _effectsProp;

    private void OnEnable()
    {
        _cardIdProp = serializedObject.FindProperty("cardID");
        _cardNameProp = serializedObject.FindProperty("cardName");
        _descriptionProp = serializedObject.FindProperty("description");
        _iconProp = serializedObject.FindProperty("icon");
        _costProp = serializedObject.FindProperty("cost");
        _conditionsProp = serializedObject.FindProperty("conditions");
        _effectsProp = serializedObject.FindProperty("effects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Card Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_cardIdProp);
        EditorGUILayout.PropertyField(_cardNameProp);
        EditorGUILayout.PropertyField(_descriptionProp);
        EditorGUILayout.PropertyField(_iconProp);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Gameplay", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_costProp);
        EditorGUILayout.PropertyField(_conditionsProp, true);
        EditorGUILayout.PropertyField(_effectsProp, true);

        DrawValidation();
        DrawQuickActions();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawValidation()
    {
        if (_costProp.intValue < 0)
            EditorGUILayout.HelpBox("Cost is negative. Card costs are expected to be >= 0.", MessageType.Warning);

        if (string.IsNullOrWhiteSpace(_cardIdProp.stringValue))
            EditorGUILayout.HelpBox("Card ID is empty. Use stable unique IDs for save/load and content references.", MessageType.Warning);

        if (string.IsNullOrWhiteSpace(_cardNameProp.stringValue))
            EditorGUILayout.HelpBox("Card Name is empty. UI will fall back to asset name.", MessageType.Info);

        if (_effectsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "Card has no effects. CardResolver will not apply anything when this card is played.",
                MessageType.Warning);
            return;
        }

        int missingEffectCount = 0;
        int missingTargetingCount = 0;
        int invalidChanceCount = 0;
        for (int i = 0; i < _effectsProp.arraySize; i++)
        {
            SerializedProperty entryProp = _effectsProp.GetArrayElementAtIndex(i);
            SerializedProperty effectProp = entryProp.FindPropertyRelative("effect");
            SerializedProperty targetingProp = entryProp.FindPropertyRelative("targeting");
            SerializedProperty chanceProp = entryProp.FindPropertyRelative("applyChance");

            if (effectProp.objectReferenceValue == null)
                missingEffectCount++;
            if (targetingProp.objectReferenceValue == null)
                missingTargetingCount++;
            if (chanceProp.floatValue < 0f || chanceProp.floatValue > 1f)
                invalidChanceCount++;
        }

        if (missingEffectCount > 0)
            EditorGUILayout.HelpBox($"Effects without BattleEffect: {missingEffectCount}. They are skipped at runtime.", MessageType.Warning);
        if (missingTargetingCount > 0)
            EditorGUILayout.HelpBox($"Effects without targeting profile: {missingTargetingCount}. They are skipped by CardResolver.", MessageType.Warning);
        if (invalidChanceCount > 0)
            EditorGUILayout.HelpBox($"Effects with invalid apply chance (expected 0..1): {invalidChanceCount}.", MessageType.Warning);
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
            {
                _cardNameProp.stringValue = target.name;
            }
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
}
