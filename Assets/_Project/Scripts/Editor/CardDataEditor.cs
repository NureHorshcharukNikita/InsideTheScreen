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
    private SerializedProperty _statusEffectsProp;

    private void OnEnable()
    {
        _cardIdProp = serializedObject.FindProperty("cardID");
        _cardNameProp = serializedObject.FindProperty("cardName");
        _descriptionProp = serializedObject.FindProperty("description");
        _iconProp = serializedObject.FindProperty("icon");
        _costProp = serializedObject.FindProperty("cost");
        _conditionsProp = serializedObject.FindProperty("conditions");
        _effectsProp = serializedObject.FindProperty("instantEffects");
        _statusEffectsProp = serializedObject.FindProperty("statusApplications");
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
        EditorGUILayout.PropertyField(_statusEffectsProp, true);

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

            emptyConditionCount += CountEmptyConditions(conditionsProp);
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

            emptyConditionCount += CountEmptyConditions(conditionsProp);
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

    private static int CountEmptyConditions(SerializedProperty conditionsProp)
    {
        if (conditionsProp == null || !conditionsProp.isArray)
            return 0;

        int emptyCount = 0;
        for (int i = 0; i < conditionsProp.arraySize; i++)
        {
            SerializedProperty conditionSpecProp = conditionsProp.GetArrayElementAtIndex(i);
            SerializedProperty conditionProp = conditionSpecProp.FindPropertyRelative("condition");

            if (conditionProp.objectReferenceValue == null)
                emptyCount++;
        }

        return emptyCount;
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
}
