using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyData))]
public class EnemyDataEditor : UnityEditor.Editor
{
    private SerializedProperty _enemyIdProp;
    private SerializedProperty _displayNameProp;
    private SerializedProperty _descriptionProp;
    private SerializedProperty _maxHealthProp;
    private SerializedProperty _abilitiesProp;

    private void OnEnable()
    {
        _enemyIdProp = serializedObject.FindProperty("enemyID");
        _displayNameProp = serializedObject.FindProperty("displayName");
        _descriptionProp = serializedObject.FindProperty("description");
        _maxHealthProp = serializedObject.FindProperty("maxHealth");
        _abilitiesProp = serializedObject.FindProperty("abilities");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Enemy Identity", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_enemyIdProp);
        EditorGUILayout.PropertyField(_displayNameProp);
        EditorGUILayout.PropertyField(_descriptionProp);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Enemy Stats", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_maxHealthProp);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Abilities", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_abilitiesProp, true);

        DrawValidation();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawValidation()
    {
        if (string.IsNullOrWhiteSpace(_enemyIdProp.stringValue))
            EditorGUILayout.HelpBox("Enemy ID is empty. Use stable unique IDs for saves and content references.", MessageType.Warning);

        if (_maxHealthProp.intValue <= 0)
            EditorGUILayout.HelpBox("Max Health should be > 0.", MessageType.Warning);

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

            if (ability.selectionWeight <= 0 || ability.effects == null || ability.effects.Count == 0)
                disabledEntries++;
        }

        if (nullEntries > 0)
            EditorGUILayout.HelpBox($"Empty ability slots: {nullEntries}.", MessageType.Warning);

        if (disabledEntries > 0)
        {
            EditorGUILayout.HelpBox(
                $"Potentially unusable abilities: {disabledEntries} (weight <= 0 or no effects).",
                MessageType.Info);
        }
    }
}
