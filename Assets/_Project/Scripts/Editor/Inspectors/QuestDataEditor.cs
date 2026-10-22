using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestData))]
public sealed class QuestDataEditor : UnityEditor.Editor
{
    private static readonly GUIContent[] ObjectiveLabels =
    {
        new GUIContent("Dialogue / Script"),
        new GUIContent("Defeat Encounter"),
        new GUIContent("Defeat Enemies"),
        new GUIContent("Collect Cards")
    };

    private static readonly int[] ObjectiveValues =
    {
        (int)QuestObjectiveType.Manual,
        (int)QuestObjectiveType.DefeatEncounter,
        (int)QuestObjectiveType.DefeatEnemies,
        (int)QuestObjectiveType.CollectCards
    };

    private SerializedProperty questKeyProp;
    private SerializedProperty titleProp;
    private SerializedProperty giverNameProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty statusProp;
    private SerializedProperty objectiveTypeProp;
    private SerializedProperty objectiveLabelProp;
    private SerializedProperty targetEncounterIdProp;
    private SerializedProperty targetEnemyProp;
    private SerializedProperty targetCardProp;
    private SerializedProperty targetCountProp;
    private SerializedProperty rewardsProp;

    private void OnEnable()
    {
        if (target == null)
            return;

        questKeyProp = serializedObject.FindProperty("questKey");
        titleProp = serializedObject.FindProperty("title");
        giverNameProp = serializedObject.FindProperty("giverName");
        descriptionProp = serializedObject.FindProperty("description");
        statusProp = serializedObject.FindProperty("status");
        objectiveTypeProp = serializedObject.FindProperty("objectiveType");
        objectiveLabelProp = serializedObject.FindProperty("objectiveLabel");
        targetEncounterIdProp = serializedObject.FindProperty("targetEncounterId");
        targetEnemyProp = serializedObject.FindProperty("targetEnemy");
        targetCardProp = serializedObject.FindProperty("targetCard");
        targetCountProp = serializedObject.FindProperty("targetCount");
        rewardsProp = serializedObject.FindProperty("rewards");
    }

    public override void OnInspectorGUI()
    {
        if (target == null)
            return;

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
        switch (objectiveType)
        {
            case QuestObjectiveType.DefeatEncounter:
                EditorGUILayout.PropertyField(targetEncounterIdProp, new GUIContent("Target Encounter Id"));
                break;
            case QuestObjectiveType.DefeatEnemies:
                EditorGUILayout.PropertyField(targetEnemyProp, new GUIContent("Enemy"));
                EditorGUILayout.PropertyField(targetCountProp, new GUIContent("Count"));
                EditorGUILayout.HelpBox(
                    "Counts every enemy of this kind defeated while the quest is active, anywhere, and " +
                    "completes the quest at the count. The giver can thank the player with a dialogue " +
                    "for the quest being Completed.",
                    MessageType.None);
                break;
            case QuestObjectiveType.CollectCards:
                EditorGUILayout.PropertyField(targetCardProp, new GUIContent("Card"));
                EditorGUILayout.PropertyField(targetCountProp, new GUIContent("Count"));
                EditorGUILayout.HelpBox(
                    "Done once the inventory holds this many: the giver's dialogue checks it with a " +
                    "Quest Objective Done condition, and its Complete Quest action takes the cards.",
                    MessageType.None);
                break;
        }
    }

    private void DrawRewardSection()
    {
        EditorGUILayout.HelpBox(
            "Each reward is a Card or Skill Points. Add several rewards to give both.\n" +
            "No reward: leave the list empty.",
            MessageType.None);
        EditorGUILayout.PropertyField(rewardsProp, new GUIContent("Rewards"), true);
    }
}
