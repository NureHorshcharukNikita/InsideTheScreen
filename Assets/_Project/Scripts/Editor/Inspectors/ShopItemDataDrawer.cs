using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ShopItemData))]
public sealed class ShopItemDataDrawer : PropertyDrawer
{
    private const float LineGap = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        SerializedProperty cardProp = property.FindPropertyRelative("card");
        SerializedProperty buyPriceModeProp = property.FindPropertyRelative("buyPriceMode");
        SerializedProperty buyPriceProp = property.FindPropertyRelative("buyPrice");
        SerializedProperty stockModeProp = property.FindPropertyRelative("stockMode");
        SerializedProperty stockProp = property.FindPropertyRelative("stock");
        SerializedProperty sellPriceModeProp = property.FindPropertyRelative("sellPriceMode");
        SerializedProperty sellPriceProp = property.FindPropertyRelative("sellPrice");

        line.y += EditorGUIUtility.singleLineHeight + LineGap;
        EditorGUI.PropertyField(line, cardProp);

        line.y += EditorGUIUtility.singleLineHeight + LineGap;
        EditorGUI.PropertyField(line, buyPriceModeProp, new GUIContent("Buy Price Mode"));

        if ((ShopPriceMode)buyPriceModeProp.enumValueIndex == ShopPriceMode.ManualPrice)
        {
            line.y += EditorGUIUtility.singleLineHeight + LineGap;
            EditorGUI.PropertyField(line, buyPriceProp, new GUIContent("Buy Price"));
        }

        line.y += EditorGUIUtility.singleLineHeight + LineGap;
        EditorGUI.PropertyField(line, stockModeProp, new GUIContent("Stock Mode"));

        if ((ShopStockMode)stockModeProp.enumValueIndex == ShopStockMode.LimitedStock)
        {
            line.y += EditorGUIUtility.singleLineHeight + LineGap;
            EditorGUI.PropertyField(line, stockProp, new GUIContent("Stock"));
        }

        line.y += EditorGUIUtility.singleLineHeight + LineGap;
        EditorGUI.PropertyField(line, sellPriceModeProp, new GUIContent("Sell Price Mode"));

        if ((ShopPriceMode)sellPriceModeProp.enumValueIndex == ShopPriceMode.ManualPrice)
        {
            line.y += EditorGUIUtility.singleLineHeight + LineGap;
            EditorGUI.PropertyField(line, sellPriceProp, new GUIContent("Sell Price"));
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lines = 1;
        if (property.isExpanded)
        {
            lines += 4;

            SerializedProperty buyPriceModeProp = property.FindPropertyRelative("buyPriceMode");
            if ((ShopPriceMode)buyPriceModeProp.enumValueIndex == ShopPriceMode.ManualPrice)
                lines++;

            SerializedProperty stockModeProp = property.FindPropertyRelative("stockMode");
            if ((ShopStockMode)stockModeProp.enumValueIndex == ShopStockMode.LimitedStock)
                lines++;

            SerializedProperty sellPriceModeProp = property.FindPropertyRelative("sellPriceMode");
            if ((ShopPriceMode)sellPriceModeProp.enumValueIndex == ShopPriceMode.ManualPrice)
                lines++;
        }

        return lines * EditorGUIUtility.singleLineHeight + (lines - 1) * LineGap;
    }
}
