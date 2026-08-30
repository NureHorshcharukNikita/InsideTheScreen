using System;
using UnityEditor;
using UnityEngine;

public static class EditorSectionGUI
{
    public static void DrawSection(string title, Action drawContent, float topSpace = 6f)
    {
        if (topSpace > 0f)
            EditorGUILayout.Space(topSpace);

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        drawContent?.Invoke();
    }

    public static void DrawTemplateHelp(string message)
    {
        EditorGUILayout.HelpBox(message, MessageType.Info);
    }

    public static void DrawReadonlyPreview(string label, string value, float minHeight)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextArea(value, GUILayout.MinHeight(minHeight));
        }
    }
}
