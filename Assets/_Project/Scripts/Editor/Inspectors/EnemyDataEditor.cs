using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyData))]
public class EnemyDataEditor : UnityEditor.Editor
{
    private SerializedProperty _profileProp;
    private SerializedProperty _battleScaleProp;
    private SerializedProperty _maxHealthProp;
    private SerializedProperty _startingArmorProp;
    private SerializedProperty _actionPointsPerTurnProp;
    private SerializedProperty _abilitiesProp;
    private SerializedProperty _startingStatusesProp;
    private SerializedProperty _lootDropsProp;
    private SerializedProperty _storyFlagOnDefeatProp;

    private void OnEnable()
    {
        if (target == null)
            return;

        _profileProp = serializedObject.FindProperty("profile");
        _battleScaleProp = serializedObject.FindProperty("battleScale");
        _maxHealthProp = serializedObject.FindProperty("maxHealth");
        _startingArmorProp = serializedObject.FindProperty("startingArmor");
        _actionPointsPerTurnProp = serializedObject.FindProperty("actionPointsPerTurn");
        _abilitiesProp = serializedObject.FindProperty("abilities");
        _startingStatusesProp = serializedObject.FindProperty("startingStatuses");
        _lootDropsProp = serializedObject.FindProperty("lootDrops");
        _storyFlagOnDefeatProp = serializedObject.FindProperty("storyFlagOnDefeat");
    }

    public override void OnInspectorGUI()
    {
        if (target == null)
            return;

        serializedObject.Update();

        EditorSectionGUI.DrawSection("Enemy Profile", DrawProfileSection, 0f);
        EditorSectionGUI.DrawSection("Enemy Stats", DrawStatsSection);
        EditorSectionGUI.DrawSection("Abilities", DrawAbilitiesSection);
        EditorSectionGUI.DrawSection("Passives", DrawPassivesSection);
        EditorSectionGUI.DrawSection("Loot Drops", DrawLootSection);
        EditorSectionGUI.DrawSection("Story", DrawStorySection);
        EditorSectionGUI.DrawSection("Validation", DrawValidation);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawProfileSection()
    {
        EditorGUILayout.PropertyField(_profileProp);
        EditorGUILayout.PropertyField(_battleScaleProp);
    }

    private void DrawStatsSection()
    {
        EditorGUILayout.PropertyField(_maxHealthProp);
        EditorGUILayout.PropertyField(_startingArmorProp);
        EditorGUILayout.PropertyField(_actionPointsPerTurnProp);
    }

    private void DrawAbilitiesSection()
    {
        EditorGUILayout.PropertyField(_abilitiesProp, true);
    }

    private void DrawPassivesSection()
    {
        EditorGUILayout.PropertyField(_startingStatusesProp, true);
    }

    private void DrawLootSection()
    {
        EditorGUILayout.PropertyField(_lootDropsProp, true);
    }

    private void DrawStorySection()
    {
        EditorGUILayout.PropertyField(_storyFlagOnDefeatProp);
    }

    private void DrawValidation()
    {
        CharacterProfile profile = _profileProp.objectReferenceValue as CharacterProfile;
        if (profile == null)
            EditorGUILayout.HelpBox("Profile is empty. Assign a CharacterProfile with ID, name, description, and sprites.", MessageType.Warning);
        else
        {
            if (string.IsNullOrWhiteSpace(profile.characterID))
                EditorGUILayout.HelpBox("Profile Character ID is empty. Use stable unique IDs for saves and content references.", MessageType.Warning);

            if (profile.PortraitSprite == null)
                EditorGUILayout.HelpBox("Profile Portrait Sprite is empty. BattleEnemyBase will keep its current sprite.", MessageType.Info);
        }

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

        int emptyLootCards = 0;
        int disabledLootEntries = 0;
        for (int i = 0; i < _lootDropsProp.arraySize; i++)
        {
            SerializedProperty dropProp = _lootDropsProp.GetArrayElementAtIndex(i);
            CardData card = dropProp.FindPropertyRelative("card").objectReferenceValue as CardData;
            float chance = dropProp.FindPropertyRelative("dropChancePercent").floatValue;

            if (card == null)
                emptyLootCards++;
            else if (chance <= 0f)
                disabledLootEntries++;
        }

        if (emptyLootCards > 0)
            EditorGUILayout.HelpBox($"Empty loot card slots: {emptyLootCards}.", MessageType.Warning);

        if (disabledLootEntries > 0)
            EditorGUILayout.HelpBox($"Loot entries with 0% chance: {disabledLootEntries}. They will never drop.", MessageType.Info);
    }
}
