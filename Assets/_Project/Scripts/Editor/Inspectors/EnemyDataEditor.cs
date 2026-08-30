using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyData))]
public class EnemyDataEditor : UnityEditor.Editor
{
    private SerializedProperty _enemyIdProp;
    private SerializedProperty _displayNameProp;
    private SerializedProperty _descriptionProp;
    private SerializedProperty _maxHealthProp;
    private SerializedProperty _startingArmorProp;
    private SerializedProperty _battleSpriteProp;
    private SerializedProperty _abilitiesProp;

    private void OnEnable()
    {
        _enemyIdProp = serializedObject.FindProperty("enemyID");
        _displayNameProp = serializedObject.FindProperty("displayName");
        _descriptionProp = serializedObject.FindProperty("description");
        _maxHealthProp = serializedObject.FindProperty("maxHealth");
        _startingArmorProp = serializedObject.FindProperty("startingArmor");
        _battleSpriteProp = serializedObject.FindProperty("battleSprite");
        _abilitiesProp = serializedObject.FindProperty("abilities");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorSectionGUI.DrawSection("Enemy Identity", DrawIdentitySection, 0f);
        EditorSectionGUI.DrawSection("Enemy Stats", DrawStatsSection);
        EditorSectionGUI.DrawSection("Battle View", DrawBattleViewSection);
        EditorSectionGUI.DrawSection("Abilities", DrawAbilitiesSection);
        EditorSectionGUI.DrawSection("Validation", DrawValidation);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentitySection()
    {
        EditorGUILayout.PropertyField(_enemyIdProp);
        EditorGUILayout.PropertyField(_displayNameProp);
        EditorGUILayout.PropertyField(_descriptionProp);
    }

    private void DrawStatsSection()
    {
        EditorGUILayout.PropertyField(_maxHealthProp);
        EditorGUILayout.PropertyField(_startingArmorProp);
    }

    private void DrawBattleViewSection()
    {
        EditorGUILayout.PropertyField(_battleSpriteProp);
    }

    private void DrawAbilitiesSection()
    {
        EditorGUILayout.PropertyField(_abilitiesProp, true);
    }

    private void DrawValidation()
    {
        if (string.IsNullOrWhiteSpace(_enemyIdProp.stringValue))
            EditorGUILayout.HelpBox("Enemy ID is empty. Use stable unique IDs for saves and content references.", MessageType.Warning);

        if (_maxHealthProp.intValue <= 0)
            EditorGUILayout.HelpBox("Max Health should be > 0.", MessageType.Warning);

        if (_battleSpriteProp.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Battle Sprite is empty. BattleEnemyBase will keep its current sprite.", MessageType.Info);

        if (_abilitiesProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No abilities assigned. EnemyBrain cannot plan actions for this enemy.", MessageType.Warning);
            return;
        }

        int nullEntries = 0;
        int disabledEntries = 0;
        for (int i = 0; i < _abilitiesProp.arraySize; i++)
        {
            SerializedProperty abilityProp = _abilitiesProp.GetArrayElementAtIndex(i);
            EnemyAbilityData ability = abilityProp.objectReferenceValue as EnemyAbilityData;
            if (ability == null)
            {
                nullEntries++;
                continue;
            }

            bool hasInstantEffects = ability.instantEffects != null && ability.instantEffects.Count > 0;
            bool hasStatusApplications = ability.statusApplications != null && ability.statusApplications.Count > 0;
            if (ability.selectionWeight <= 0 || (!hasInstantEffects && !hasStatusApplications))
                disabledEntries++;
        }

        if (nullEntries > 0)
            EditorGUILayout.HelpBox($"Empty ability slots: {nullEntries}.", MessageType.Warning);

        if (disabledEntries > 0)
        {
            EditorGUILayout.HelpBox(
                $"Potentially unusable abilities: {disabledEntries} (weight <= 0 or no instant effects/status applications).",
                MessageType.Info);
        }
    }
}
