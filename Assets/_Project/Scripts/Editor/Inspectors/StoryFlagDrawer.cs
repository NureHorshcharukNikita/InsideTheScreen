using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Dropdown for [StoryFlag] fields. The flags are collected from the saved project files
// (dialogue actions, NPC conditions, story events), so a new flag shows up here
// as soon as the asset or scene that sets it is saved. Each chapter of the story is a submenu, its
// flags in story order, as StoryFlagOrder.txt next to this file has them: the main line numbered,
// side quests marked, states that come and go (where Hideshi waits) on their own. Flags it does not
// list yet come last. Only the dropdown changes: the flags keep their names, so saves keep working.
[CustomPropertyDrawer(typeof(StoryFlagAttribute))]
public sealed class StoryFlagDrawer : PropertyDrawer
{
    private const string NoneOption = "(none)";
    private const string ProjectFolder = "Assets/_Project";
    private const string OrderFile = ProjectFolder + "/Scripts/Editor/Inspectors/StoryFlagOrder.txt";
    private const string UnorderedTitle = "Not in the order yet";

    private static readonly Regex FlagLine = new(
        @"^\s*(?:storyFlag|doneFlag|hiddenAfterStoryFlag|followAfterStoryFlag): (\S+)\s*$");

    private sealed class Chapter
    {
        public string Title;
        public readonly List<(string flag, string label)> Flags = new();
    }

    private static List<string> knownFlags;
    private static List<Chapter> chapters;
    private static Dictionary<string, string> labels;

    [InitializeOnLoadMethod]
    private static void ResetCacheOnProjectChange()
    {
        EditorApplication.projectChanged += () =>
        {
            knownFlags = null;
            chapters = null;
        };
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        label = EditorGUI.BeginProperty(position, label, property);
        Rect field = EditorGUI.PrefixLabel(position, label);
        string value = property.stringValue;
        string shown = property.hasMultipleDifferentValues ? "—" : LabelOf(value);
        if (EditorGUI.DropdownButton(field, new GUIContent(shown), FocusType.Keyboard))
            ShowMenu(field, property.serializedObject.targetObjects, property.propertyPath, value);
        EditorGUI.EndProperty();
    }

    private static string LabelOf(string flag)
    {
        if (string.IsNullOrEmpty(flag))
            return NoneOption;

        GetChapters();
        return labels.TryGetValue(flag, out string label) ? label : flag;
    }

    // One submenu per chapter, opening on hover; picking a flag writes it to every selected object.
    private static void ShowMenu(Rect field, Object[] targets, string path, string value)
    {
        GenericMenu menu = new();
        menu.AddItem(new GUIContent(NoneOption), string.IsNullOrEmpty(value), () => Pick(targets, path, string.Empty));
        menu.AddSeparator(string.Empty);
        foreach (Chapter chapter in GetChapters())
        {
            foreach ((string flag, string label) in chapter.Flags)
                menu.AddItem(new GUIContent($"{chapter.Title}/{label}"), flag == value, () => Pick(targets, path, flag));
        }

        // A flag nothing uses any more still shows, so it can be seen and changed.
        if (!string.IsNullOrEmpty(value) && !labels.ContainsKey(value))
        {
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent(value), true, () => Pick(targets, path, value));
        }

        menu.DropDown(field);
    }

    // The menu answers after this OnGUI is over, so through a serialized object of its own.
    private static void Pick(Object[] targets, string path, string flag)
    {
        targets = System.Array.FindAll(targets, target => target != null);
        if (targets.Length == 0)
            return;

        SerializedObject owner = new(targets);
        SerializedProperty property = owner.FindProperty(path);
        if (property == null)
            return;

        property.stringValue = flag;
        owner.ApplyModifiedProperties();
    }

    // "## Title" starts a chapter, "flag" is the main line (numbered), "- flag" a side quest,
    // "* flag" a state; "#" starts a note. Only flags the project uses are listed.
    private static List<Chapter> GetChapters()
    {
        if (chapters != null)
            return chapters;

        HashSet<string> known = new(GetKnownFlags());
        HashSet<string> placed = new();
        List<Chapter> ordered = new();
        labels = new Dictionary<string, string>();
        Chapter chapter = null;
        int step = 0;

        string[] lines = File.Exists(OrderFile) ? File.ReadAllLines(OrderFile) : new string[0];
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.StartsWith("##"))
            {
                chapter = new Chapter { Title = line.Substring(2).Trim() };
                ordered.Add(chapter);
                continue;
            }

            int note = line.IndexOf('#');
            if (note >= 0)
                line = line.Substring(0, note).Trim();

            char kind = line.Length > 0 ? line[0] : ' ';
            string flag = kind is '-' or '*' ? line.Substring(1).Trim() : line;
            if (flag.Length == 0 || !known.Contains(flag) || !placed.Add(flag))
                continue;

            if (chapter == null)
            {
                chapter = new Chapter { Title = "Story" };
                ordered.Add(chapter);
            }

            string label = kind == '-' ? $"{flag}  (side)" : kind == '*' ? flag : $"{++step:00}  {flag}";
            chapter.Flags.Add((flag, label));
            labels[flag] = label;
        }

        chapters = ordered.FindAll(candidate => candidate.Flags.Count > 0);
        for (int i = 0; i < chapters.Count; i++)
            chapters[i].Title = $"{i + 1}. {chapters[i].Title}";

        Chapter unordered = new() { Title = UnorderedTitle };
        foreach (string flag in GetKnownFlags())
        {
            if (placed.Contains(flag))
                continue;

            unordered.Flags.Add((flag, flag));
            labels[flag] = flag;
        }

        if (unordered.Flags.Count > 0)
            chapters.Add(unordered);

        return chapters;
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
