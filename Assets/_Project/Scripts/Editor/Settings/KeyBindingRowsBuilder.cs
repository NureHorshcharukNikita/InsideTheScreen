using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Gives the Keyboard Bindings list a row for every action that has none yet, in the pause menu
// settings (InventoryWindow prefab) and in the main menu settings: a copy of the last row with the
// action's icon. Then lays all rows out in GameInputSettings.SettingsOrder. Run once after adding
// an action; a list that has all its rows is left as it is.
public static class KeyBindingRowsBuilder
{
    private const string InventoryWindowPrefabPath = "Assets/_Project/Prefabs/UI/InventoryWindow.prefab";
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenuScene.unity";
    private const string RowNamePrefix = "Binding_";
    private const string IconName = "Icon";
    private const float RowStep = 102f;

    // The page icons of the inventory window and the arrows of the dialogue box. "path#name" is a
    // sprite of a sheet.
    private static readonly Dictionary<GameInputAction, string> IconPaths = new()
    {
        { GameInputAction.Quests, "Assets/_Project/Art/Inventory/BookTask.png" },
        { GameInputAction.Crafting, "Assets/_Project/Art/Inventory/CraftTable.png" },
        { GameInputAction.Map, "Assets/_Project/Art/UI/Settings/SettingBindingIcons.png#SettingBindingIcons_6" },
        { GameInputAction.Skills, "Assets/_Project/Art/Inventory/Skills.png" },
        { GameInputAction.DialogueNext, "Assets/_Project/Art/UI/Dialogue/SkipArrow.png" },
        { GameInputAction.DialogueSkip, "Assets/_Project/Art/UI/Dialogue/SkipDialogueArrow.png" }
    };

    [MenuItem("Tools/UI/Add Missing Key Binding Rows")]
    private static void AddMissingRows()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Leave Play Mode before adding key binding rows.");
            return;
        }

        int added = AddToInventoryWindow() + AddToMainMenu();
        EditorUtility.DisplayDialog(
            "Key Binding Rows",
            added > 0
                ? $"Rows added: {added}. The pause menu and the main menu settings list every action now."
                : "Every Keyboard Bindings list already has all its rows.",
            "OK");
    }

    private static int AddToInventoryWindow()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(InventoryWindowPrefabPath);
        try
        {
            int added = AddRows(root);
            if (added > 0)
                PrefabUtility.SaveAsPrefabAsset(root, InventoryWindowPrefabPath);

            return added;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // An open main menu scene is only marked changed, so its other unsaved changes stay the user's
    // to save; a closed one is opened, saved and closed again.
    private static int AddToMainMenu()
    {
        Scene scene = SceneManager.GetSceneByPath(MainMenuScenePath);
        bool openedHere = !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);

        int added = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
            added += AddRows(root);

        if (added > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (openedHere)
                EditorSceneManager.SaveScene(scene);
        }

        if (openedHere)
            EditorSceneManager.CloseScene(scene, true);

        return added;
    }

    private static int AddRows(GameObject root)
    {
        int added = 0;
        foreach (SettingsInputPanel panel in root.GetComponentsInChildren<SettingsInputPanel>(true))
            added += AddRows(panel);

        return added;
    }

    private static int AddRows(SettingsInputPanel panel)
    {
        List<SettingsKeyBindingRow> rows = panel.GetComponentsInChildren<SettingsKeyBindingRow>(true).ToList();
        if (rows.Count == 0)
        {
            Debug.LogWarning($"{panel.name} has no key binding row to copy.", panel);
            return 0;
        }

        SettingsKeyBindingRow template = rows[rows.Count - 1];
        RectTransform content = (RectTransform)template.transform.parent;
        int added = 0;

        foreach (GameInputAction action in GameInputSettings.SettingsOrder)
        {
            if (rows.Exists(row => row.Action == action))
                continue;

            SettingsKeyBindingRow row = Object.Instantiate(template, content);
            row.name = RowNamePrefix + action;

            SerializedObject serializedRow = new(row);
            serializedRow.FindProperty("action").intValue = (int)action;
            serializedRow.ApplyModifiedPropertiesWithoutUndo();

            SetIcon(row, action);
            rows.Add(row);
            added++;
        }

        if (added == 0)
            return 0;

        List<SettingsKeyBindingRow> ordered = rows.OrderBy(row => GetOrder(row.Action)).ToList();
        LayOut(content, ordered);

        SerializedObject serializedPanel = new(panel);
        SerializedProperty bindingRows = serializedPanel.FindProperty("bindingRows");
        bindingRows.arraySize = ordered.Count;
        for (int i = 0; i < ordered.Count; i++)
            bindingRows.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];

        serializedPanel.ApplyModifiedPropertiesWithoutUndo();
        return added;
    }

    private static int GetOrder(GameInputAction action)
    {
        int index = Array.IndexOf(GameInputSettings.SettingsOrder, action);
        return index < 0 ? int.MaxValue : index;
    }

    // One row under another, centred on the list as the rows already are; the list grows to fit
    // them and keeps its top edge where it was.
    private static void LayOut(RectTransform content, List<SettingsKeyBindingRow> rows)
    {
        float oldHeight = content.sizeDelta.y;
        float height = rows.Count * RowStep;
        Vector2 contentPosition = content.anchoredPosition;
        contentPosition.y += (1f - content.pivot.y) * (oldHeight - height);
        content.sizeDelta = new Vector2(content.sizeDelta.x, height);
        content.anchoredPosition = contentPosition;

        for (int i = 0; i < rows.Count; i++)
        {
            RectTransform rect = (RectTransform)rows[i].transform;
            rect.SetSiblingIndex(i);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, height * 0.5f - RowStep * (i + 0.5f));
        }
    }

    private static void SetIcon(SettingsKeyBindingRow row, GameInputAction action)
    {
        Image icon = row.transform.Find(IconName)?.GetComponent<Image>();
        Sprite sprite = IconPaths.TryGetValue(action, out string path) ? LoadSprite(path) : null;
        if (icon == null || sprite == null)
        {
            Debug.LogWarning($"No icon for the {action} key binding row; it keeps the copied one.", row);
            return;
        }

        icon.sprite = sprite;
        icon.preserveAspect = true;
    }

    private static Sprite LoadSprite(string path)
    {
        string[] parts = path.Split('#');
        if (parts.Length == 1)
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);

        return AssetDatabase.LoadAllAssetsAtPath(parts[0])
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == parts[1]);
    }
}
