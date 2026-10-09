using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(QuestRewardData))]
public sealed class QuestRewardDataDrawer : PropertyDrawer
{
    private const float LineGap = 2f;

    // Art/Inventory/SkillPoint.png, looked up by GUID so moving the file does not break it.
    private const string SkillPointIconGuid = "e45fa93a340f6a143917827c1a66b21f";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty typeProp = property.FindPropertyRelative("type");
        SerializedProperty cardProp = property.FindPropertyRelative("card");
        SerializedProperty cardCountProp = property.FindPropertyRelative("cardCount");
        SerializedProperty skillPointsProp = property.FindPropertyRelative("skillPoints");
        QuestRewardType type = (QuestRewardType)typeProp.enumValueIndex;

        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, GetSummary(type, cardProp, cardCountProp, skillPointsProp), true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            line.y += EditorGUIUtility.singleLineHeight + LineGap;
            EditorGUI.PropertyField(line, typeProp, new GUIContent("Type"));
            type = (QuestRewardType)typeProp.enumValueIndex;

            if (type == QuestRewardType.Card)
            {
                line.y += EditorGUIUtility.singleLineHeight + LineGap;
                EditorGUI.PropertyField(line, cardProp, new GUIContent("Card"));

                line.y += EditorGUIUtility.singleLineHeight + LineGap;
                EditorGUI.PropertyField(line, cardCountProp, new GUIContent("Count"));
            }
            else
            {
                line.y += EditorGUIUtility.singleLineHeight + LineGap;
                EditorGUI.PropertyField(line, skillPointsProp, new GUIContent("Amount"));
            }

            EditorGUI.indentLevel--;
        }

        ApplySkillPointIcon(property, type);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lines = 1;
        if (property.isExpanded)
        {
            SerializedProperty typeProp = property.FindPropertyRelative("type");
            lines += (QuestRewardType)typeProp.enumValueIndex == QuestRewardType.Card ? 3 : 2;
        }

        return lines * EditorGUIUtility.singleLineHeight + (lines - 1) * LineGap;
    }

    private static string GetSummary(
        QuestRewardType type,
        SerializedProperty cardProp,
        SerializedProperty cardCountProp,
        SerializedProperty skillPointsProp)
    {
        if (type == QuestRewardType.SkillPoints)
            return $"Skill Points: +{skillPointsProp.intValue}";

        Object card = cardProp.objectReferenceValue;
        return card != null
            ? $"Card: {card.name} x{cardCountProp.intValue}"
            : "Card: (none)";
    }

    // The icon field is hidden: skill point rewards get the light bulb, card rewards keep it
    // empty so the card's own icon is shown.
    private static void ApplySkillPointIcon(SerializedProperty property, QuestRewardType type)
    {
        Sprite skillPointIcon = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(SkillPointIconGuid));
        if (skillPointIcon == null)
            return;

        SerializedProperty iconProp = property.FindPropertyRelative("icon");
        if (type == QuestRewardType.SkillPoints && iconProp.objectReferenceValue == null)
            iconProp.objectReferenceValue = skillPointIcon;
        else if (type == QuestRewardType.Card && iconProp.objectReferenceValue == skillPointIcon)
            iconProp.objectReferenceValue = null;
    }
}
