using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyAbilityData))]
public class EnemyAbilityDataEditor : UnityEditor.Editor
{
    private SerializedProperty _abilityIdProp;
    private SerializedProperty _displayNameProp;
    private SerializedProperty _descriptionProp;
    private SerializedProperty _iconProp;
    private SerializedProperty _selectionWeightProp;
    private SerializedProperty _intentSummaryProp;
    private SerializedProperty _cooldownTurnsProp;
    private SerializedProperty _maxUsesProp;
    private SerializedProperty _conditionsProp;
    private SerializedProperty _effectsProp;

    private void OnEnable()
    {
        _abilityIdProp = serializedObject.FindProperty("abilityID");
        _displayNameProp = serializedObject.FindProperty("displayName");
        _descriptionProp = serializedObject.FindProperty("description");
        _iconProp = serializedObject.FindProperty("icon");
        _selectionWeightProp = serializedObject.FindProperty("selectionWeight");
        _intentSummaryProp = serializedObject.FindProperty("intentSummary");
        _cooldownTurnsProp = serializedObject.FindProperty("cooldownTurns");
        _maxUsesProp = serializedObject.FindProperty("maxUses");
        _conditionsProp = serializedObject.FindProperty("conditions");
        _effectsProp = serializedObject.FindProperty("effects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Enemy Ability", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_abilityIdProp);
        EditorGUILayout.PropertyField(_displayNameProp);
        EditorGUILayout.PropertyField(_descriptionProp);
        EditorGUILayout.PropertyField(_iconProp);
        EditorGUILayout.PropertyField(_intentSummaryProp);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Selection Rules", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_selectionWeightProp);
        EditorGUILayout.PropertyField(_cooldownTurnsProp);
        EditorGUILayout.PropertyField(_maxUsesProp);

        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(_conditionsProp, true);
        EditorGUILayout.PropertyField(_effectsProp, true);

        DrawValidation();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawValidation()
    {
        if (string.IsNullOrWhiteSpace(_abilityIdProp.stringValue))
            EditorGUILayout.HelpBox("Ability ID is empty. Use stable unique IDs for saves and content references.", MessageType.Warning);

        if (_selectionWeightProp.intValue <= 0)
            EditorGUILayout.HelpBox("Selection Weight should be > 0, otherwise this ability is effectively never picked.", MessageType.Warning);

        if (_cooldownTurnsProp.intValue < 0)
            EditorGUILayout.HelpBox("Cooldown Turns should be >= 0.", MessageType.Warning);

        if (_maxUsesProp.intValue < -1)
            EditorGUILayout.HelpBox("Max Uses should be -1 (unlimited) or >= 0.", MessageType.Warning);

        if (_effectsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Ability has no effects. Executing it will do nothing.", MessageType.Warning);
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
            EditorGUILayout.HelpBox($"Effect specs without BattleEffect: {missingEffectCount}. They are skipped by executor.", MessageType.Warning);
        if (missingTargetingCount > 0)
            EditorGUILayout.HelpBox($"Effect specs without targeting profile: {missingTargetingCount}. They are skipped by executor.", MessageType.Warning);
        if (invalidChanceCount > 0)
            EditorGUILayout.HelpBox($"Effect specs with invalid apply chance (expected 0..1): {invalidChanceCount}.", MessageType.Warning);
    }
}
