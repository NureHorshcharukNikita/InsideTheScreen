using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleEffectDisplayData))]
public class BattleEffectDisplayDataEditor : UnityEditor.Editor
{
    private SerializedProperty _useCategoryIconProp;
    private SerializedProperty _iconCategoryProp;
    private SerializedProperty _iconProp;
    private SerializedProperty _badgeModeProp;
    private SerializedProperty _badgeValueSourceProp;
    private SerializedProperty _badgeValueFormatProp;
    private SerializedProperty _titleProp;
    private SerializedProperty _tooltipFormatProp;

    private const int PreviewValue = 3;
    private const int PreviewDuration = 2;
    private const int PreviewStacks = 2;

    private void OnEnable()
    {
        if (target == null)
            return;

        _useCategoryIconProp = serializedObject.FindProperty("useCategoryIcon");
        _iconCategoryProp = serializedObject.FindProperty("iconCategory");
        _iconProp = serializedObject.FindProperty("icon");
        _badgeModeProp = serializedObject.FindProperty("badgeMode");
        _badgeValueSourceProp = serializedObject.FindProperty("badgeValueSource");
        _badgeValueFormatProp = serializedObject.FindProperty("badgeValueFormat");
        _titleProp = serializedObject.FindProperty("title");
        _tooltipFormatProp = serializedObject.FindProperty("tooltipFormat");
    }

    public override void OnInspectorGUI()
    {
        if (target == null)
            return;

        serializedObject.Update();

        EditorSectionGUI.DrawSection("Badge", () =>
        {
            DrawIcon();
            EditorGUILayout.PropertyField(_badgeModeProp);

            BattleEffectBadgeMode badgeMode = (BattleEffectBadgeMode)_badgeModeProp.enumValueIndex;
            if (badgeMode == BattleEffectBadgeMode.ValueSource)
                EditorGUILayout.PropertyField(_badgeValueSourceProp);
            else
                EditorGUILayout.PropertyField(_badgeValueFormatProp);

            DrawTemplateHelp();
            DrawBadgePreview();
        }, 0f);

        EditorSectionGUI.DrawSection("Tooltip", () =>
        {
            EditorGUILayout.PropertyField(_titleProp);
            EditorGUILayout.PropertyField(_tooltipFormatProp);
            DrawTooltipPreview();
        });

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIcon()
    {
        EditorGUILayout.PropertyField(_useCategoryIconProp);
        if (!_useCategoryIconProp.boolValue)
        {
            EditorGUILayout.PropertyField(_iconProp);
            return;
        }

        EditorGUILayout.PropertyField(_iconCategoryProp);
        if (((BattleEffectDisplayData)target).CategoryIcon == null)
        {
            EditorGUILayout.HelpBox(
                $"Category {_iconCategoryProp.enumDisplayNames[_iconCategoryProp.enumValueIndex]} has no icon in " +
                $"Resources/{CardCategoryIcons.ResourceName}; the fallback icon is shown instead.",
                MessageType.Info);
        }

        EditorGUILayout.PropertyField(_iconProp, new GUIContent("Fallback Icon"));
    }

    private void DrawTemplateHelp()
    {
        EditorSectionGUI.DrawTemplateHelp(
            "Custom Format and Tooltip support: {value}, {total}, {duration}, {stacks}, {badge}. " +
            "Value Source uses {badge}. Preview uses value=3, total=6, duration=2, stacks=2.");
    }

    private void DrawBadgePreview()
    {
        BattleEffectBadgeMode badgeMode = (BattleEffectBadgeMode)_badgeModeProp.enumValueIndex;
        string template = badgeMode == BattleEffectBadgeMode.CustomFormat
            ? _badgeValueFormatProp.stringValue
            : "{badge}";
        if (string.IsNullOrWhiteSpace(template))
            template = "{badge}";

        int badgeValue = GetPreviewBadgeValue();
        string preview = BattleEffectDisplayFormatter.Format(template, PreviewValue, PreviewDuration, PreviewStacks, badgeValue);
        EditorSectionGUI.DrawReadonlyPreview("Badge Preview", preview, 20f);
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

        EditorSectionGUI.DrawReadonlyPreview("Tooltip Preview", preview, 52f);
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

}
