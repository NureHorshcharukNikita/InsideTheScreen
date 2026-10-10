using UnityEditor;
using UnityEngine;

// One line for a story event target: the kind (Player / Npc / Scene Object) and, next to it,
// the NpcData or the scene object.
[CustomPropertyDrawer(typeof(StoryTarget))]
public sealed class StoryTargetDrawer : PropertyDrawer
{
    private const float TypeWidth = 105f;
    private const float Gap = 4f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty typeProp = property.FindPropertyRelative("type");
        Rect content = EditorGUI.PrefixLabel(position, label);

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        Rect typeRect = new(content.x, content.y, Mathf.Min(TypeWidth, content.width), content.height);
        EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

        Rect valueRect = new(typeRect.xMax + Gap, content.y, content.xMax - typeRect.xMax - Gap, content.height);
        switch ((StoryTargetType)typeProp.enumValueIndex)
        {
            case StoryTargetType.Npc:
                EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("npc"), GUIContent.none);
                break;
            case StoryTargetType.SceneObject:
                EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("sceneObject"), GUIContent.none);
                break;
        }

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
