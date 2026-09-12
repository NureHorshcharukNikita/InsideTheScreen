using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NpcDialogueEntry))]
public sealed class NpcDialogueEntryDrawer : PropertyDrawer
{
    private const float RowSpacing = 7f;
    private const float SectionSpacing = 14f;
    private const float BottomPadding = 16f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect row = GetRow(position, 0f, LineHeight);
        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, GetFoldoutLabel(property, label), true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = LineHeight + RowSpacing;

            DrawProperty(position, ref y, property, "key", "Key");
            DrawProperty(position, ref y, property, "dialogue", "Dialogue");
            DrawProperty(position, ref y, property, "repeatable", "Can Repeat");

            DrawSection(position, ref y, "Availability");
            SerializedProperty talkTimingProp = property.FindPropertyRelative("talkTiming");
            DrawProperty(position, ref y, talkTimingProp, "Talk Timing");
            DrawTimingProperties(position, ref y, property, talkTimingProp);

            DrawSection(position, ref y, "Quest Condition");
            SerializedProperty requireQuestStatusProp = property.FindPropertyRelative("requireQuestStatus");
            DrawProperty(position, ref y, requireQuestStatusProp, "Require Quest Status");
            if (requireQuestStatusProp.boolValue)
            {
                DrawProperty(position, ref y, property, "quest", "Quest");
                DrawProperty(position, ref y, property, "questStatus", "Quest Status");
            }

            DrawSection(position, ref y, "Story Flag Condition");
            SerializedProperty storyFlagProp = property.FindPropertyRelative("requiredStoryFlag");
            DrawProperty(position, ref y, storyFlagProp, "Required Story Flag");
            if (!string.IsNullOrWhiteSpace(storyFlagProp.stringValue))
                DrawProperty(position, ref y, property, "invertStoryFlag", "Invert Story Flag");

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = LineHeight;

        if (property.isExpanded)
        {
            height += RowSpacing;
            AddPropertyHeight(ref height, property.FindPropertyRelative("key"), "Key");
            AddPropertyHeight(ref height, property.FindPropertyRelative("dialogue"), "Dialogue");
            AddPropertyHeight(ref height, property.FindPropertyRelative("repeatable"), "Can Repeat");

            AddSectionHeight(ref height);
            SerializedProperty talkTimingProp = property.FindPropertyRelative("talkTiming");
            AddPropertyHeight(ref height, talkTimingProp, "Talk Timing");
            AddTimingHeight(ref height, property, talkTimingProp);

            AddSectionHeight(ref height);
            SerializedProperty requireQuestStatusProp = property.FindPropertyRelative("requireQuestStatus");
            AddPropertyHeight(ref height, requireQuestStatusProp, "Require Quest Status");
            if (requireQuestStatusProp.boolValue)
            {
                AddPropertyHeight(ref height, property.FindPropertyRelative("quest"), "Quest");
                AddPropertyHeight(ref height, property.FindPropertyRelative("questStatus"), "Quest Status");
            }

            AddSectionHeight(ref height);
            SerializedProperty storyFlagProp = property.FindPropertyRelative("requiredStoryFlag");
            AddPropertyHeight(ref height, storyFlagProp, "Required Story Flag");
            if (!string.IsNullOrWhiteSpace(property.FindPropertyRelative("requiredStoryFlag").stringValue))
                AddPropertyHeight(ref height, property.FindPropertyRelative("invertStoryFlag"), "Invert Story Flag");

            height += BottomPadding;
        }

        return height;
    }

    [System.Obsolete]
    public override bool CanCacheInspectorGUI(SerializedProperty property)
    {
        return false;
    }

    private static GUIContent GetFoldoutLabel(SerializedProperty property, GUIContent fallbackLabel)
    {
        SerializedProperty keyProp = property.FindPropertyRelative("key");
        if (keyProp != null && !string.IsNullOrWhiteSpace(keyProp.stringValue))
            return new GUIContent(keyProp.stringValue);

        return fallbackLabel;
    }

    private static void DrawTimingProperties(
        Rect position,
        ref float y,
        SerializedProperty property,
        SerializedProperty talkTimingProp)
    {
        NpcDialogueTalkTiming timing = (NpcDialogueTalkTiming)talkTimingProp.enumValueIndex;

        switch (timing)
        {
            case NpcDialogueTalkTiming.ConversationNumber:
                DrawProperty(position, ref y, property, "conversationNumber", "Conversation Number");
                break;
            case NpcDialogueTalkTiming.ConversationRange:
                DrawProperty(position, ref y, property, "rangeStartConversation", "From Conversation");
                DrawProperty(position, ref y, property, "rangeEndConversation", "To Conversation (0 = forever)");
                break;
        }
    }

    private static void DrawSection(Rect position, ref float y, string title)
    {
        y += SectionSpacing;
        Rect row = GetRow(position, y, LineHeight);
        EditorGUI.LabelField(row, title, EditorStyles.boldLabel);
        y += LineHeight + RowSpacing;
    }

    private static void DrawProperty(Rect position, ref float y, SerializedProperty parent, string propertyName, string label)
    {
        DrawProperty(position, ref y, parent.FindPropertyRelative(propertyName), label);
    }

    private static void DrawProperty(Rect position, ref float y, SerializedProperty property, string label)
    {
        GUIContent content = new(label);
        float height = EditorGUI.GetPropertyHeight(property, content, true);
        Rect row = GetRow(position, y, height);
        EditorGUI.PropertyField(row, property, content, true);
        y += height + RowSpacing;
    }

    private static Rect GetRow(Rect position, float y, float height)
    {
        return new Rect(position.x, position.y + y, position.width, height);
    }

    private static float LineHeight => EditorGUIUtility.singleLineHeight;

    private static void AddSectionHeight(ref float height)
    {
        height += SectionSpacing + LineHeight + RowSpacing;
    }

    private static void AddPropertyHeight(ref float height, SerializedProperty property, string label)
    {
        height += EditorGUI.GetPropertyHeight(property, new GUIContent(label), true) + RowSpacing;
    }

    private static void AddTimingHeight(ref float height, SerializedProperty property, SerializedProperty talkTimingProp)
    {
        NpcDialogueTalkTiming timing = (NpcDialogueTalkTiming)talkTimingProp.enumValueIndex;

        switch (timing)
        {
            case NpcDialogueTalkTiming.ConversationNumber:
                AddPropertyHeight(ref height, property.FindPropertyRelative("conversationNumber"), "Conversation Number");
                break;
            case NpcDialogueTalkTiming.ConversationRange:
                AddPropertyHeight(ref height, property.FindPropertyRelative("rangeStartConversation"), "From Conversation");
                AddPropertyHeight(ref height, property.FindPropertyRelative("rangeEndConversation"), "To Conversation (0 = forever)");
                break;
        }
    }
}
