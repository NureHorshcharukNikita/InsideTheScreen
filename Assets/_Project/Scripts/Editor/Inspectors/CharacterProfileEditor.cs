using UnityEditor;

[CustomEditor(typeof(CharacterProfile))]
public sealed class CharacterProfileEditor : UnityEditor.Editor
{
    private SerializedProperty characterIdProp;
    private SerializedProperty displayNameProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty portraitSpriteProp;
    private SerializedProperty portraitFacingProp;
    private SerializedProperty portraitHeightProp;
    private SerializedProperty mapSpriteProp;
    private SerializedProperty voiceProp;

    private void OnEnable()
    {
        if (target == null)
            return;

        characterIdProp = serializedObject.FindProperty("characterID");
        displayNameProp = serializedObject.FindProperty("displayName");
        descriptionProp = serializedObject.FindProperty("description");
        portraitSpriteProp = serializedObject.FindProperty("portraitSprite");
        portraitFacingProp = serializedObject.FindProperty("portraitFacing");
        portraitHeightProp = serializedObject.FindProperty("portraitHeight");
        mapSpriteProp = serializedObject.FindProperty("mapSprite");
        voiceProp = serializedObject.FindProperty("voice");
    }

    public override void OnInspectorGUI()
    {
        if (target == null)
            return;

        serializedObject.Update();

        EditorSectionGUI.DrawSection("Identity", DrawIdentitySection, 0f);
        EditorSectionGUI.DrawSection("Portrait", DrawPortraitSection);
        EditorSectionGUI.DrawSection("Map", DrawMapSection);
        EditorSectionGUI.DrawSection("Dialogue", DrawDialogueSection);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentitySection()
    {
        EditorGUILayout.PropertyField(characterIdProp);
        EditorGUILayout.PropertyField(displayNameProp);
        EditorGUILayout.PropertyField(descriptionProp);
    }

    private void DrawPortraitSection()
    {
        EditorGUILayout.PropertyField(portraitSpriteProp);
        EditorGUILayout.PropertyField(portraitFacingProp);
        if (portraitHeightProp != null)
            EditorGUILayout.PropertyField(portraitHeightProp);
    }

    private void DrawMapSection()
    {
        EditorGUILayout.PropertyField(mapSpriteProp);
    }

    private void DrawDialogueSection()
    {
        EditorGUILayout.PropertyField(voiceProp);
    }
}
