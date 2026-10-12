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
    private SerializedProperty _actionPointCostProp;
    private SerializedProperty _cooldownTurnsProp;
    private SerializedProperty _maxUsesProp;
    private SerializedProperty _conditionsProp;
    private SerializedProperty _effectsProp;
    private SerializedProperty _statusEffectsProp;

    private void OnEnable()
    {
        if (target == null)
            return;

        _abilityIdProp = serializedObject.FindProperty("abilityID");
        _displayNameProp = serializedObject.FindProperty("displayName");
        _descriptionProp = serializedObject.FindProperty("description");
        _iconProp = serializedObject.FindProperty("icon");
        _selectionWeightProp = serializedObject.FindProperty("selectionWeight");
        _intentSummaryProp = serializedObject.FindProperty("intentSummary");
        _actionPointCostProp = serializedObject.FindProperty("actionPointCost");
        _cooldownTurnsProp = serializedObject.FindProperty("cooldownTurns");
        _maxUsesProp = serializedObject.FindProperty("maxUses");
        _conditionsProp = serializedObject.FindProperty("conditions");
        _effectsProp = serializedObject.FindProperty("instantEffects");
        _statusEffectsProp = serializedObject.FindProperty("statusApplications");
    }

    public override void OnInspectorGUI()
    {
        if (target == null)
            return;

        serializedObject.Update();

        EditorSectionGUI.DrawSection("Enemy Ability", DrawIdentitySection, 0f);
        EditorSectionGUI.DrawSection("Selection Rules", DrawSelectionRulesSection);
        EditorSectionGUI.DrawSection("Battle Actions", DrawBattleActionsSection);
        EditorSectionGUI.DrawSection("Validation", DrawValidation);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentitySection()
    {
        EnemyAbilityData ability = target as EnemyAbilityData;
        EditorGUILayout.PropertyField(_abilityIdProp);
        EditorGUILayout.PropertyField(_displayNameProp);
        EditorGUILayout.PropertyField(_descriptionProp);
        DrawDescriptionTemplateHelp();
        EditorSectionGUI.DrawReadonlyPreview("Formatted Description", ability?.Description, 24f);
        EditorGUILayout.PropertyField(_iconProp);
        EditorGUILayout.PropertyField(_intentSummaryProp);
        EditorSectionGUI.DrawReadonlyPreview("Formatted Intent", ability?.IntentSummary, 24f);
    }

    private void DrawSelectionRulesSection()
    {
        EditorGUILayout.PropertyField(_selectionWeightProp);
        EditorGUILayout.PropertyField(_actionPointCostProp);
        EditorGUILayout.PropertyField(_cooldownTurnsProp);
        EditorGUILayout.PropertyField(_maxUsesProp);
    }

    private void DrawBattleActionsSection()
    {
        EditorGUILayout.PropertyField(_conditionsProp, true);
        EditorGUILayout.PropertyField(_effectsProp, true);
        EditorGUILayout.PropertyField(_statusEffectsProp, true);
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

        if (_effectsProp.arraySize == 0 && _statusEffectsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Ability has no instant effects or status applications. Executing it will do nothing.", MessageType.Warning);
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
            EditorGUILayout.HelpBox($"Instant Effects without InstantBattleEffect: {missingEffectCount}. They are skipped by executor.", MessageType.Warning);
        if (missingStatusCount > 0)
            EditorGUILayout.HelpBox($"Status applications without BattleStatus: {missingStatusCount}. They are skipped by executor.", MessageType.Warning);
        if (missingTargetingCount > 0)
            EditorGUILayout.HelpBox($"Entries without targeting profile: {missingTargetingCount}. They are skipped by executor.", MessageType.Warning);
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
            "Description and Intent Summary support: {value}, {total}, {duration}, {stacks}, {chance}, {effect}, {status}. " +
            "For exact entries use: {instant0.value}, {instant0.total}, {instant0.effect}, {status0.value}, {status0.total}, {status0.status}, {status0.duration}.");
    }

}
