using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Draws a [SerializeReference, SubclassPicker] field, or each element of such a list, as a
// foldout titled by the value's ToString() with a dropdown on the right to pick its class.
// The dropdown lists every [Serializable] class deriving from the field type, grouped by
// their [SubclassMenu] paths (e.g. the steps of a StoryEvent).
[CustomPropertyDrawer(typeof(SubclassPickerAttribute))]
public sealed class SubclassPickerDrawer : PropertyDrawer
{
    private const float MaxButtonWidth = 150f;
    private const float Gap = 4f;
    private const string ArrayDataPrefix = ".Array.data[";

    private static readonly Dictionary<Type, List<Type>> TypesByBase = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        SeparateSharedReference(property);
        object value = property.managedReferenceValue;

        EditorGUI.BeginProperty(position, label, property);

        Rect header = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        float buttonWidth = Mathf.Min(MaxButtonWidth, header.width * 0.4f);
        Rect buttonRect = new(header.xMax - buttonWidth, header.y, buttonWidth, header.height);
        Rect titleRect = new(header.x, header.y, header.width - buttonWidth - Gap, header.height);

        string buttonText = value != null ? GetMenuName(value.GetType()) : "Choose...";
        if (EditorGUI.DropdownButton(buttonRect, new GUIContent(buttonText), FocusType.Keyboard))
            ShowMenu(buttonRect, property, GetBaseType());

        string title = GetTitle(property, value, label);
        if (value == null)
        {
            EditorGUI.LabelField(titleRect, title);
        }
        else
        {
            property.isExpanded = EditorGUI.Foldout(titleRect, property.isExpanded, title, true);
            if (property.isExpanded)
                DrawChildren(position, property);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
            return EditorGUI.GetPropertyHeight(property, label, true);

        float height = EditorGUIUtility.singleLineHeight;
        if (property.managedReferenceValue == null || !property.isExpanded)
            return height;

        foreach (SerializedProperty child in GetChildren(property))
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);

        return height;
    }

    private static void DrawChildren(Rect position, SerializedProperty property)
    {
        float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.indentLevel++;
        foreach (SerializedProperty child in GetChildren(property))
        {
            float height = EditorGUI.GetPropertyHeight(child, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true);
            y += height + EditorGUIUtility.standardVerticalSpacing;
        }
        EditorGUI.indentLevel--;
    }

    private static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
    {
        SerializedProperty child = property.Copy();
        SerializedProperty end = property.GetEndProperty();
        if (!child.NextVisible(true))
            yield break;

        while (!SerializedProperty.EqualContents(child, end))
        {
            yield return child.Copy();
            if (!child.NextVisible(false))
                yield break;
        }
    }

    // "+" on a list copies the last element, and with [SerializeReference] the copy can point to
    // the same object, so editing one step would change both. Such a copy gets its own object.
    private static void SeparateSharedReference(SerializedProperty property)
    {
        int index = GetArrayIndex(property.propertyPath, out string arrayPath);
        if (index <= 0)
            return;

        object value = property.managedReferenceValue;
        SerializedProperty array = property.serializedObject.FindProperty(arrayPath);
        if (value == null || array == null || !array.isArray)
            return;

        long id = property.managedReferenceId;
        for (int i = 0; i < index && i < array.arraySize; i++)
        {
            if (array.GetArrayElementAtIndex(i).managedReferenceId != id)
                continue;

            object copy = Activator.CreateInstance(value.GetType());
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(value), copy);
            property.managedReferenceValue = copy;
            return;
        }
    }

    private Type GetBaseType()
    {
        Type type = fieldInfo.FieldType;
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return type.GetGenericArguments()[0];

        return type;
    }

    private static void ShowMenu(Rect rect, SerializedProperty property, Type baseType)
    {
        SerializedObject serializedObject = property.serializedObject;
        string path = property.propertyPath;
        Type current = property.managedReferenceValue?.GetType();

        GenericMenu menu = new();
        menu.AddItem(new GUIContent("(None)"), current == null, () => Assign(serializedObject, path, null));
        menu.AddSeparator(string.Empty);
        foreach (Type type in GetTypes(baseType))
            menu.AddItem(new GUIContent(GetMenuPath(type)), type == current, () => Assign(serializedObject, path, type));

        menu.DropDown(rect);
    }

    private static void Assign(SerializedObject serializedObject, string path, Type type)
    {
        serializedObject.Update();
        SerializedProperty property = serializedObject.FindProperty(path);
        if (property == null)
            return;

        property.managedReferenceValue = type != null ? Activator.CreateInstance(type) : null;
        property.isExpanded = type != null;
        serializedObject.ApplyModifiedProperties();
    }

    private static List<Type> GetTypes(Type baseType)
    {
        if (TypesByBase.TryGetValue(baseType, out List<Type> types))
            return types;

        types = TypeCache.GetTypesDerivedFrom(baseType)
            .Where(IsPickable)
            .OrderBy(GetMenuPath, StringComparer.Ordinal)
            .ToList();

        if (IsPickable(baseType))
            types.Insert(0, baseType);

        TypesByBase[baseType] = types;
        return types;
    }

    private static bool IsPickable(Type type)
    {
        return !type.IsAbstract
            && !type.IsGenericType
            && type.IsSerializable
            && !typeof(UnityEngine.Object).IsAssignableFrom(type)
            && type.GetConstructor(Type.EmptyTypes) != null;
    }

    private static string GetMenuPath(Type type)
    {
        SubclassMenuAttribute menu = (SubclassMenuAttribute)Attribute.GetCustomAttribute(type, typeof(SubclassMenuAttribute));
        return menu != null && !string.IsNullOrWhiteSpace(menu.Path)
            ? menu.Path
            : ObjectNames.NicifyVariableName(type.Name);
    }

    private static string GetMenuName(Type type)
    {
        string path = GetMenuPath(type);
        int slash = path.LastIndexOf('/');
        return slash >= 0 ? path.Substring(slash + 1) : path;
    }

    // "3. Hideshi walks to FatherStandPoint" for list elements, "Label: ..." for single fields.
    private static string GetTitle(SerializedProperty property, object value, GUIContent label)
    {
        string text = value == null
            ? "(empty)"
            : HasOwnToString(value.GetType()) ? value.ToString() : GetMenuName(value.GetType());

        int index = GetArrayIndex(property.propertyPath, out _);
        if (index >= 0)
            return $"{index + 1}. {text}";

        return value == null ? label.text : $"{label.text}: {text}";
    }

    private static bool HasOwnToString(Type type)
    {
        return type.GetMethod(nameof(ToString), Type.EmptyTypes)?.DeclaringType != typeof(object);
    }

    // "steps.Array.data[3]" -> 3 and "steps"; -1 when the property is not a list element.
    private static int GetArrayIndex(string path, out string arrayPath)
    {
        arrayPath = null;
        int start = path.LastIndexOf(ArrayDataPrefix, StringComparison.Ordinal);
        if (start < 0 || !path.EndsWith("]", StringComparison.Ordinal))
            return -1;

        int indexStart = start + ArrayDataPrefix.Length;
        if (!int.TryParse(path.Substring(indexStart, path.Length - indexStart - 1), out int index))
            return -1;

        arrayPath = path.Substring(0, start);
        return index;
    }
}
