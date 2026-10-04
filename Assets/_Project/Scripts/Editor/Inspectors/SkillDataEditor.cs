using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillData))]
public sealed class SkillDataEditor : UnityEditor.Editor
{
    private SerializedProperty skillIdProp;
    private SerializedProperty displayNameProp;
    private SerializedProperty categoryProp;
    private SerializedProperty treeOrderProp;
    private SerializedProperty iconProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty ranksProp;
    private SerializedProperty prerequisitesProp;
    private SerializedProperty progressionRequirementsProp;

    private void OnEnable()
    {
        skillIdProp = serializedObject.FindProperty("skillId");
        displayNameProp = serializedObject.FindProperty("displayName");
        categoryProp = serializedObject.FindProperty("category");
        treeOrderProp = serializedObject.FindProperty("treeOrder");
        iconProp = serializedObject.FindProperty("icon");
        descriptionProp = serializedObject.FindProperty("description");
        ranksProp = serializedObject.FindProperty("ranks");
        prerequisitesProp = serializedObject.FindProperty("prerequisites");
        progressionRequirementsProp = serializedObject.FindProperty("progressionRequirements");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorSectionGUI.DrawSection("Identity", DrawIdentity, 0f);
        EditorSectionGUI.DrawSection("Ranks", DrawRanks);
        EditorSectionGUI.DrawSection("Unlock Requirements", DrawRequirements);
        EditorSectionGUI.DrawSection("Validation", DrawValidation);

        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("Tools/Skills/Create Skill")]
    private static void CreateSkill()
    {
        SkillData skill = EditorAssetUtility.CreateAsset<SkillData>(
            "Assets/_Project/ScriptableObjects/Skills",
            "NewSkill");
        Selection.activeObject = skill;
        EditorGUIUtility.PingObject(skill);
    }

    private void DrawIdentity()
    {
        EditorGUILayout.PropertyField(skillIdProp, new GUIContent("Skill ID"));
        EditorGUILayout.PropertyField(displayNameProp);
        EditorGUILayout.PropertyField(categoryProp);
        EditorGUILayout.PropertyField(treeOrderProp, new GUIContent("Tree Order"));
        EditorGUILayout.PropertyField(iconProp);
        EditorGUILayout.PropertyField(descriptionProp);
    }

    private void DrawRanks()
    {
        EditorGUILayout.PropertyField(ranksProp, new GUIContent("Rank Progression"), true);
        EditorGUILayout.HelpBox(
            "Each list entry is one rank. Configure its cost and effects manually.",
            MessageType.Info);
    }

    private void DrawRequirements()
    {
        EditorGUILayout.PropertyField(prerequisitesProp, new GUIContent("Required Skills"), true);
        EditorGUILayout.PropertyField(
            progressionRequirementsProp,
            new GUIContent("Required Game Progress"),
            true);
    }

    private void DrawValidation()
    {
        SkillData skill = target as SkillData;
        if (string.IsNullOrWhiteSpace(skillIdProp.stringValue))
            EditorGUILayout.HelpBox("Skill ID is required for saves and prerequisites.", MessageType.Error);
        else if (HasDuplicateId(skill))
            EditorGUILayout.HelpBox("Another SkillData asset uses the same Skill ID.", MessageType.Error);

        if (ranksProp.arraySize == 0)
            EditorGUILayout.HelpBox("Add at least one rank.", MessageType.Warning);

        for (int i = 0; i < prerequisitesProp.arraySize; i++)
        {
            SerializedProperty entry = prerequisitesProp.GetArrayElementAtIndex(i);
            SkillData requiredSkill = entry.FindPropertyRelative("skill").objectReferenceValue as SkillData;
            if (requiredSkill == skill)
                EditorGUILayout.HelpBox("A skill cannot require itself.", MessageType.Error);
        }
    }

    private bool HasDuplicateId(SkillData current)
    {
        List<SkillData> skills = EditorAssetUtility.LoadAssets<SkillData>();
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != current && skills[i].SkillId == skillIdProp.stringValue)
                return true;
        }

        return false;
    }
}

[CustomPropertyDrawer(typeof(SkillEffectData))]
public sealed class SkillEffectDataDrawer : PropertyDrawer
{
    private const float Gap = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty kind = property.FindPropertyRelative("kind");
        int rows = kind.enumValueIndex == (int)SkillEffectKind.StatModifier ? 4 : 2;
        return rows * EditorGUIUtility.singleLineHeight + (rows - 1) * Gap;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        SerializedProperty kind = property.FindPropertyRelative("kind");
        EditorGUI.PropertyField(NextRow(ref position), kind, label);

        if (kind.enumValueIndex == (int)SkillEffectKind.StatModifier)
        {
            EditorGUI.PropertyField(NextRow(ref position), property.FindPropertyRelative("stat"));
            EditorGUI.PropertyField(NextRow(ref position), property.FindPropertyRelative("modifierType"));
            EditorGUI.PropertyField(NextRow(ref position), property.FindPropertyRelative("value"));
        }
        else
        {
            EditorGUI.PropertyField(NextRow(ref position), property.FindPropertyRelative("passive"));
        }

        EditorGUI.EndProperty();
    }

    private static Rect NextRow(ref Rect position)
    {
        Rect row = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        position.y += EditorGUIUtility.singleLineHeight + Gap;
        return row;
    }
}

[CustomPropertyDrawer(typeof(SkillUnlockRequirement))]
public sealed class SkillUnlockRequirementDrawer : PropertyDrawer
{
    private const float Gap = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SkillUnlockRequirementType type = (SkillUnlockRequirementType)property
            .FindPropertyRelative("type")
            .enumValueIndex;
        int rows = type == SkillUnlockRequirementType.QuestStatus ? 3 : 2;
        return rows * EditorGUIUtility.singleLineHeight + (rows - 1) * Gap;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        SerializedProperty typeProp = property.FindPropertyRelative("type");
        EditorGUI.PropertyField(NextRow(ref position), typeProp, label);

        switch ((SkillUnlockRequirementType)typeProp.enumValueIndex)
        {
            case SkillUnlockRequirementType.StoryFlag:
                EditorGUI.PropertyField(NextRow(ref position), property.FindPropertyRelative("storyFlag"));
                break;
            case SkillUnlockRequirementType.QuestStatus:
                EditorGUI.PropertyField(NextRow(ref position), property.FindPropertyRelative("quest"));
                EditorGUI.PropertyField(NextRow(ref position), property.FindPropertyRelative("requiredQuestStatus"));
                break;
            case SkillUnlockRequirementType.DefeatedEncounter:
                EditorGUI.PropertyField(NextRow(ref position), property.FindPropertyRelative("encounterId"));
                break;
        }

        EditorGUI.EndProperty();
    }

    private static Rect NextRow(ref Rect position)
    {
        Rect row = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        position.y += EditorGUIUtility.singleLineHeight + Gap;
        return row;
    }
}
