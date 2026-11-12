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
        new GUIContent("Collect Cards"),
        new GUIContent("Story Flags")
    };

    private static readonly int[] ObjectiveValues =
    {
        (int)QuestObjectiveType.Manual,
        (int)QuestObjectiveType.DefeatEncounter,
        (int)QuestObjectiveType.DefeatEnemies,
        (int)QuestObjectiveType.CollectCards,
        (int)QuestObjectiveType.StoryFlags
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
    private SerializedProperty targetFlagsProp;
    private SerializedProperty stepsProp;
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
        targetFlagsProp = serializedObject.FindProperty("targetFlags");
        stepsProp = serializedObject.FindProperty("steps");
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
            case QuestObjectiveType.StoryFlags:
                EditorGUILayout.PropertyField(targetFlagsProp, new GUIContent("Flags"), true);
                EditorGUILayout.HelpBox(
                    "Counts the flags set, e.g. the things gathered for Survival Basics (2/4). The quest " +
                    "is completed by a dialogue or a story event, e.g. the one showing them to the giver.",
                    MessageType.None);
                break;
        }

        EditorGUILayout.PropertyField(stepsProp, new GUIContent("Steps"), true);
        if (stepsProp.arraySize > 0)
        {
            EditorGUILayout.HelpBox(
                "Steps instead of Objective Label, one stage after another. A step is done once its flag " +
                "is set, or, without a flag, once the count is reached (never in the last stage: the quest " +
                "is completed instead). With Previous puts a step in the stage of the one before it: done " +
                "at once, in any order. The quest tracker shows the stage under way; the quest log every " +
                "stage reached, the oldest at the top.",
                MessageType.None);
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
