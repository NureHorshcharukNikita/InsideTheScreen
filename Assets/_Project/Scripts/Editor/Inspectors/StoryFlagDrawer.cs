using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Dropdown for [StoryFlag] fields. The flags are collected from the saved project files
// (dialogue actions, NPC conditions, story events), so a new flag shows up here
// as soon as the asset or scene that sets it is saved.
[CustomPropertyDrawer(typeof(StoryFlagAttribute))]
public sealed class StoryFlagDrawer : PropertyDrawer
{
    private const string NoneOption = "(none)";
    private const string ProjectFolder = "Assets/_Project";

    private static readonly Regex FlagLine = new(
        @"^\s*(?:storyFlag|doneFlag|hiddenAfterStoryFlag|followAfterStoryFlag): (\S+)\s*$");

    private static List<string> knownFlags;

    [InitializeOnLoadMethod]
    private static void ResetCacheOnProjectChange()
    {
        EditorApplication.projectChanged += () => knownFlags = null;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        List<string> options = new() { NoneOption };
        options.AddRange(GetKnownFlags());

        string value = property.stringValue;
        if (!string.IsNullOrEmpty(value) && !options.Contains(value))
            options.Add(value);

        int index = string.IsNullOrEmpty(value) ? 0 : options.IndexOf(value);

        EditorGUI.BeginProperty(position, label, property);
        int selected = EditorGUI.Popup(position, label.text, index, options.ToArray());
        if (selected != index)
            property.stringValue = selected == 0 ? string.Empty : options[selected];
        EditorGUI.EndProperty();
    }

    private static IReadOnlyList<string> GetKnownFlags()
    {
        if (knownFlags != null)
            return knownFlags;

        SortedSet<string> flags = new();
        foreach (string path in Directory.EnumerateFiles(ProjectFolder, "*.*", SearchOption.AllDirectories))
        {
            if (!path.EndsWith(".asset") && !path.EndsWith(".prefab") && !path.EndsWith(".unity"))
                continue;

            foreach (string line in File.ReadLines(path))
            {
                Match match = FlagLine.Match(line);
                if (match.Success)
                    flags.Add(match.Groups[1].Value);
            }
        }

        knownFlags = new List<string>(flags);
        return knownFlags;
    }
}
