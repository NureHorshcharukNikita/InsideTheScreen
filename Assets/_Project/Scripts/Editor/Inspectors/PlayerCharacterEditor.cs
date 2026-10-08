using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerCharacter))]
public sealed class PlayerCharacterEditor : UnityEditor.Editor
{
    private void OnEnable()
    {
        SkillProgression.Changed += OnSkillProgressionChanged;
    }

    private void OnDisable()
    {
        SkillProgression.Changed -= OnSkillProgressionChanged;
    }

    private void OnSkillProgressionChanged()
    {
        if (target != null)
            Repaint();
    }

    public override void OnInspectorGUI()
    {
        // Target can already be destroyed (e.g. when exiting Play Mode or unloading a scene).
        if (target == null)
            return;

        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Skill Progression (Runtime)", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode to view or change Current Skill Points.",
                MessageType.Info);
            return;
        }

        using (new EditorGUI.DisabledScope(!SkillProgression.IsInitialized))
        {
            EditorGUI.BeginChangeCheck();
            int points = EditorGUILayout.IntField(
                "Current Skill Points",
                SkillProgression.AvailablePoints);

            if (EditorGUI.EndChangeCheck())
                SkillProgression.SetAvailablePoints(points);
        }
    }
}
