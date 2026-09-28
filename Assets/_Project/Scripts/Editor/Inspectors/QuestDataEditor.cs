using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestData))]
public sealed class QuestDataEditor : UnityEditor.Editor
{
    private static readonly GUIContent[] ObjectiveLabels =
    {
        new GUIContent("Dialogue / Script"),
        new GUIContent("Defeat Encounter")
    };

    private static readonly int[] ObjectiveValues =
    {
        (int)QuestObjectiveType.Manual,
        (int)QuestObjectiveType.DefeatEncounter
    };

    private SerializedProperty questKeyProp;
    private SerializedProperty titleProp;
    private SerializedProperty giverNameProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty statusProp;
    private SerializedProperty objectiveTypeProp;
    private SerializedProperty objectiveLabelProp;
    private SerializedProperty targetEncounterIdProp;
    private SerializedProperty rewardsProp;
    private SerializedProperty rewardLabelProp;
    private SerializedProperty rewardIconProp;
    private SerializedProperty rewardCardProp;
    private SerializedProperty rewardCardCountProp;

    private void OnEnable()
    {
        questKeyProp = serializedObject.FindProperty("questKey");
        titleProp = serializedObject.FindProperty("title");
        giverNameProp = serializedObject.FindProperty("giverName");
        descriptionProp = serializedObject.FindProperty("description");
        statusProp = serializedObject.FindProperty("status");
        objectiveTypeProp = serializedObject.FindProperty("objectiveType");
        objectiveLabelProp = serializedObject.FindProperty("objectiveLabel");
        targetEncounterIdProp = serializedObject.FindProperty("targetEncounterId");
        rewardsProp = serializedObject.FindProperty("rewards");
        rewardLabelProp = serializedObject.FindProperty("rewardLabel");
        rewardIconProp = serializedObject.FindProperty("rewardIcon");
        rewardCardProp = serializedObject.FindProperty("rewardCard");
        rewardCardCountProp = serializedObject.FindProperty("rewardCardCount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorSectionGUI.DrawSection("Identity", DrawIdentitySection, 0f);
        EditorSectionGUI.DrawSection("State", DrawStateSection);
        EditorSectionGUI.DrawSection("Objective", DrawObjectiveSection);
        EditorSectionGUI.DrawSection("Reward", DrawRewardSection);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentitySection()
    {
        EditorGUILayout.PropertyField(questKeyProp);
        EditorGUILayout.PropertyField(titleProp);
        EditorGUILayout.PropertyField(giverNameProp);
        EditorGUILayout.PropertyField(descriptionProp);
    }

    private void DrawStateSection()
    {
        EditorGUILayout.PropertyField(statusProp);
    }

    private void DrawObjectiveSection()
    {
        objectiveTypeProp.enumValueIndex = EditorGUILayout.IntPopup(
            new GUIContent("Objective Type"),
            objectiveTypeProp.enumValueIndex,
            ObjectiveLabels,
            ObjectiveValues);

        EditorGUILayout.PropertyField(objectiveLabelProp, new GUIContent("Objective Label"));

        QuestObjectiveType objectiveType = (QuestObjectiveType)objectiveTypeProp.enumValueIndex;
        if (objectiveType == QuestObjectiveType.DefeatEncounter)
            EditorGUILayout.PropertyField(targetEncounterIdProp, new GUIContent("Target Encounter Id"));
    }

    private void DrawRewardSection()
    {
        EditorGUILayout.PropertyField(rewardsProp, new GUIContent("Rewards"), true);

        if (rewardsProp.arraySize > 0)
            return;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Legacy Reward", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rewardLabelProp, new GUIContent("Label"));
        EditorGUILayout.PropertyField(rewardIconProp, new GUIContent("Icon"));
        EditorGUILayout.PropertyField(rewardCardProp, new GUIContent("Card"));
        EditorGUILayout.PropertyField(rewardCardCountProp, new GUIContent("Card Count"));
    }
}
