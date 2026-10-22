using UnityEditor;
using UnityEngine;

// One line per condition: the kind, then the story flag or the quest (and its status), then Invert.
[CustomPropertyDrawer(typeof(StoryCondition))]
public sealed class StoryConditionDrawer : PropertyDrawer
{
    private const float TypeWidth = 100f;
    private const float InvertWidth = 58f;
    private const float Gap = 4f;

    private static readonly GUIContent InvertLabel = new(
        "Invert",
        "Met when the opposite is true: the flag is not set, the quest is not in this status.");

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        int indent = EditorGUI.indentLevel;
        Rect line = EditorGUI.IndentedRect(position);
        EditorGUI.indentLevel = 0;

        SerializedProperty typeProp = property.FindPropertyRelative("type");
        SerializedProperty invertProp = property.FindPropertyRelative("invert");

        Rect typeRect = new(line.x, line.y, TypeWidth, line.height);
        Rect invertRect = new(line.xMax - InvertWidth, line.y, InvertWidth, line.height);
        Rect valueRect = new(typeRect.xMax + Gap, line.y, invertRect.x - typeRect.xMax - Gap * 2f, line.height);

        EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

        if ((StoryConditionType)typeProp.enumValueIndex == StoryConditionType.QuestStatus)
        {
            float questWidth = valueRect.width * 0.6f;
            Rect questRect = new(valueRect.x, valueRect.y, questWidth - Gap, valueRect.height);
            Rect statusRect = new(valueRect.x + questWidth, valueRect.y, valueRect.width - questWidth, valueRect.height);
            EditorGUI.PropertyField(questRect, property.FindPropertyRelative("quest"), GUIContent.none);
            EditorGUI.PropertyField(statusRect, property.FindPropertyRelative("questStatus"), GUIContent.none);
        }
        else if ((StoryConditionType)typeProp.enumValueIndex == StoryConditionType.QuestObjectiveDone)
        {
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("quest"), GUIContent.none);
        }
        else
        {
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("storyFlag"), GUIContent.none);
        }

        EditorGUI.BeginChangeCheck();
        bool invert = EditorGUI.ToggleLeft(invertRect, InvertLabel, invertProp.boolValue);
        if (EditorGUI.EndChangeCheck())
            invertProp.boolValue = invert;

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
