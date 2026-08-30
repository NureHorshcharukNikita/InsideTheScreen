using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleEffectDisplayData))]
public class BattleEffectDisplayDataEditor : UnityEditor.Editor
{
    private SerializedProperty _iconProp;
    private SerializedProperty _fallbackLabelProp;
    private SerializedProperty _badgeValueSourceProp;
    private SerializedProperty _badgeValueFormatProp;
    private SerializedProperty _titleProp;
    private SerializedProperty _tooltipFormatProp;

    private const int PreviewValue = 3;
    private const int PreviewDuration = 2;
    private const int PreviewStacks = 1;

    private void OnEnable()
    {
        _iconProp = serializedObject.FindProperty("icon");
        _fallbackLabelProp = serializedObject.FindProperty("fallbackLabel");
        _badgeValueSourceProp = serializedObject.FindProperty("badgeValueSource");
        _badgeValueFormatProp = serializedObject.FindProperty("badgeValueFormat");
        _titleProp = serializedObject.FindProperty("title");
        _tooltipFormatProp = serializedObject.FindProperty("tooltipFormat");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Badge", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_iconProp);
        EditorGUILayout.PropertyField(_fallbackLabelProp);
        EditorGUILayout.PropertyField(_badgeValueSourceProp);
        EditorGUILayout.PropertyField(_badgeValueFormatProp);

        DrawTemplateHelp();
        DrawBadgePreview();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Tooltip", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_titleProp);
        EditorGUILayout.PropertyField(_tooltipFormatProp);

        DrawTooltipPreview();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTemplateHelp()
    {
        EditorGUILayout.HelpBox(
            "Badge Value Format and Tooltip support: {value}, {duration}, {stacks}. " +
            "Preview uses value=3, duration=2, stacks=1.",
            MessageType.Info);
    }

    private void DrawBadgePreview()
    {
        string template = string.IsNullOrWhiteSpace(_badgeValueFormatProp.stringValue)
            ? "{value}"
            : _badgeValueFormatProp.stringValue;

        int value = GetPreviewBadgeValue();
        string preview = BattleEffectDisplayFormatter.Format(template, value, PreviewDuration, PreviewStacks);
        DrawReadonlyPreview("Badge Preview", preview, 20f);
    }

    private void DrawTooltipPreview()
    {
        if (string.IsNullOrWhiteSpace(_tooltipFormatProp.stringValue))
            return;

        string preview = BattleEffectDisplayFormatter.Format(
            _tooltipFormatProp.stringValue,
            PreviewValue,
            PreviewDuration,
            PreviewStacks);

        DrawReadonlyPreview("Tooltip Preview", preview, 52f);
    }

    private int GetPreviewBadgeValue()
    {
        BattleEffectBadgeValueSource source = (BattleEffectBadgeValueSource)_badgeValueSourceProp.enumValueIndex;
        return source switch
        {
            BattleEffectBadgeValueSource.Duration => PreviewDuration,
            BattleEffectBadgeValueSource.Stacks => PreviewStacks,
            BattleEffectBadgeValueSource.StacksThenDuration => PreviewStacks > 1 ? PreviewStacks : PreviewDuration,
            _ => PreviewValue
        };
    }

    private static void DrawReadonlyPreview(string label, string value, float minHeight)
    {
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextArea(value, GUILayout.MinHeight(minHeight));
        }
    }
}
