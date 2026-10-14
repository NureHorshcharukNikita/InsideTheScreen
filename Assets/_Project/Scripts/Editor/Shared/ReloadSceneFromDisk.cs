using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Reopens the open scene from its file when the file was changed outside the editor (e.g. by a
// tool). Otherwise Unity keeps the old version open: Play runs the old version, and saving would
// overwrite the file with it. Without unsaved changes it reloads at once; with them it asks first.
// Saving from the editor does not count as a change from outside.
public sealed class ReloadSceneFromDisk : AssetPostprocessor
{
    // A save from the editor re-imports the scene too: those are not changes from outside.
    private const double SaveGraceSeconds = 3d;

    private static readonly Dictionary<string, double> SavedAt = new();

    [InitializeOnLoadMethod]
    private static void WatchSaves()
    {
        EditorSceneManager.sceneSaved += scene => SavedAt[scene.path] = EditorApplication.timeSinceStartup;
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path) || System.Array.IndexOf(importedAssets, scene.path) < 0)
            return;

        if (SavedAt.TryGetValue(scene.path, out double savedAt)
            && EditorApplication.timeSinceStartup - savedAt < SaveGraceSeconds)
            return;

        string path = scene.path;
        EditorApplication.delayCall += () => Reload(path);
    }

    private static void Reload(string path)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != path)
            return;

        if (scene.isDirty && !EditorUtility.DisplayDialog(
                "Reload scene from disk",
                $"{scene.name} was changed outside the editor, and it has unsaved changes here. Reload it from " +
                "the file? Keep it to stay with the version open now (saving it overwrites the file).",
                "Reload",
                "Keep"))
            return;

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log($"Reloaded {scene.name} from disk.");
    }
}
