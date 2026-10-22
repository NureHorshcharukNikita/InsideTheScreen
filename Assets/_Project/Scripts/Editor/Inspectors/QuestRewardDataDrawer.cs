using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(QuestRewardData))]
public sealed class QuestRewardDataDrawer : PropertyDrawer
{
    private const float LineGap = 2f;

    // Art/Inventory/SkillPoint.png and Gold.png, looked up by GUID so moving the files does not break it.
    private const string SkillPointIconGuid = "e45fa93a340f6a143917827c1a66b21f";
    private const string GoldIconGuid = "9f7d23e22527dc148997f9a29ed64ff3";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty typeProp = property.FindPropertyRelative("type");
        SerializedProperty cardProp = property.FindPropertyRelative("card");
        SerializedProperty cardCountProp = property.FindPropertyRelative("cardCount");
        SerializedProperty skillPointsProp = property.FindPropertyRelative("skillPoints");
        SerializedProperty goldProp = property.FindPropertyRelative("gold");
        QuestRewardType type = (QuestRewardType)typeProp.enumValueIndex;

        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, GetSummary(type, cardProp, cardCountProp, skillPointsProp, goldProp), true);

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
                EditorGUI.PropertyField(line, type == QuestRewardType.Gold ? goldProp : skillPointsProp, new GUIContent("Amount"));
            }

            EditorGUI.indentLevel--;
        }

        ApplyRewardIcon(property, type);
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
        SerializedProperty skillPointsProp,
        SerializedProperty goldProp)
    {
        if (type == QuestRewardType.SkillPoints)
            return $"Skill Points: +{skillPointsProp.intValue}";

        if (type == QuestRewardType.Gold)
            return $"Gold: +{goldProp.intValue}";

        Object card = cardProp.objectReferenceValue;
        return card != null
            ? $"Card: {card.name} x{cardCountProp.intValue}"
            : "Card: (none)";
    }

    // The icon field is hidden: skill point rewards get the light bulb, gold rewards the coin, card
    // rewards keep it empty so the card's own icon is shown.
    private static void ApplyRewardIcon(SerializedProperty property, QuestRewardType type)
    {
        Sprite skillPointIcon = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(SkillPointIconGuid));
        Sprite goldIcon = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(GoldIconGuid));
        Sprite wanted = type switch
        {
            QuestRewardType.SkillPoints => skillPointIcon,
            QuestRewardType.Gold => goldIcon,
            _ => null
        };

        SerializedProperty iconProp = property.FindPropertyRelative("icon");
        Object current = iconProp.objectReferenceValue;
        bool isRewardIcon = current == null || current == skillPointIcon || current == goldIcon;
        if (isRewardIcon && current != wanted)
            iconProp.objectReferenceValue = wanted;
    }
}
