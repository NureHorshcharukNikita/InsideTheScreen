using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opens a scene that is open in the editor again when its file changes on disk from outside (the
// scene YAML patched as text, a git checkout). Otherwise play mode goes on with the old version from
// memory, and a later Ctrl+S writes that old version over the new file. Unity's own saves are told
// apart by the file's time, remembered when the scene is opened or saved. In play mode it waits until
// play stops; with unsaved changes in the scene it asks first.
// A file Temp/ReloadOpenScenes asks for the same at the next script reload, for a scene patched
// together with scripts: Unity imports the scene before the scripts compile, before this runs.
[InitializeOnLoad]
public sealed class ExternalSceneChangeReloader : AssetPostprocessor
{
    private const string PendingKey = "ExternalSceneChangeReloader.Pending";
    private const string KnownTimeKey = "ExternalSceneChangeReloader.KnownTime:";
    private const string MarkerPath = "Temp/ReloadOpenScenes";

    static ExternalSceneChangeReloader()
    {
        EditorSceneManager.sceneOpened += (scene, _) => RememberFileTime(scene.path);
        EditorSceneManager.sceneSaved += scene => RememberFileTime(scene.path);
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                ScheduleReload();
        };

        if (File.Exists(MarkerPath))
        {
            File.Delete(MarkerPath);
            for (int i = 0; i < SceneManager.sceneCount; i++)
                AddPending(SceneManager.GetSceneAt(i).path);
        }

        ScheduleReload();
    }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        bool any = false;
        foreach (string path in imported)
        {
            if (!path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || !SceneManager.GetSceneByPath(path).isLoaded)
                continue;

            if (SessionState.GetString(KnownTimeKey + path, string.Empty) == FileTime(path))
                continue;

            AddPending(path);
            any = true;
        }

        if (any)
            ScheduleReload();
    }

    private static void ScheduleReload()
    {
        EditorApplication.delayCall -= ReloadPending;
        EditorApplication.delayCall += ReloadPending;
    }

    private static void ReloadPending()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        List<string> pending = GetPending();
        SessionState.EraseString(PendingKey);
        foreach (string path in pending)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            if (!scene.isLoaded)
                continue;

            if (SceneManager.sceneCount > 1)
            {
                Debug.LogWarning($"{path} was changed on disk. Open it again to get the new version; saving it now would write the old one over it.");
                continue;
            }

            if (scene.isDirty && !EditorUtility.DisplayDialog("Scene changed on disk",
                    $"{scene.name} was changed on disk, but the open scene has unsaved changes.\n\n" +
                    "Reload it from disk? The unsaved changes are lost. Keeping it and saving would write the old version over the new file.",
                    "Reload from disk", "Keep the open scene"))
                continue;

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Debug.Log($"{path} was changed on disk and has been opened again.");
        }
    }

    private static void RememberFileTime(string path)
    {
        if (!string.IsNullOrEmpty(path))
            SessionState.SetString(KnownTimeKey + path, FileTime(path));
    }

    private static string FileTime(string path)
    {
        return File.Exists(path) ? File.GetLastWriteTimeUtc(path).Ticks.ToString() : string.Empty;
    }

    private static List<string> GetPending()
    {
        string stored = SessionState.GetString(PendingKey, string.Empty);
        return new List<string>(stored.Split('\n', StringSplitOptions.RemoveEmptyEntries));
    }

    private static void AddPending(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        List<string> pending = GetPending();
        if (!pending.Contains(path))
            pending.Add(path);

        SessionState.SetString(PendingKey, string.Join("\n", pending));
    }
}
