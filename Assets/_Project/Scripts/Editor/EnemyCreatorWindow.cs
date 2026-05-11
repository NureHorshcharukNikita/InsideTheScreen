using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EnemyCreatorWindow : EditorWindow
{
    private const string DefaultEnemiesFolder = "Assets/_Project/ScriptableObjects/Enemies";
    private const string DefaultAbilitiesFolder = "Assets/_Project/ScriptableObjects/Enemies/Abilities";

    private readonly List<EnemyData> _enemies = new();
    private readonly List<EnemyAbilityData> _abilities = new();

    private int _tabIndex;
    private int _selectedEnemyIndex = -1;
    private int _selectedAbilityIndex = -1;

    private string _enemiesFolder = DefaultEnemiesFolder;
    private string _abilitiesFolder = DefaultAbilitiesFolder;
    private string _newEnemyName = "NewEnemyData";
    private string _newAbilityName = "NewEnemyAbility";
    private string _enemySearch = string.Empty;
    private string _abilitySearch = string.Empty;

    private Vector2 _leftScroll;
    private Vector2 _rightScroll;
    private Editor _selectedAssetEditor;

    [MenuItem("Tools/Enemies/Enemy Database Editor")]
    public static void Open()
    {
        GetWindow<EnemyCreatorWindow>("Enemy Database");
    }

    private void OnEnable()
    {
        RefreshAssets();
    }

    private void OnDisable()
    {
        if (_selectedAssetEditor != null)
            DestroyImmediate(_selectedAssetEditor);
    }

    private void OnGUI()
    {
        DrawTopBar();
        _tabIndex = GUILayout.Toolbar(_tabIndex, new[] { "Enemies", "Abilities" });
        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (_tabIndex == 0)
                DrawEnemiesPanel();
            else
                DrawAbilitiesPanel();

            DrawInspectorPanel();
        }
    }

    private void DrawTopBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Enemy Tools", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();

            bool hasSelection = _selectedEnemyIndex >= 0 || _selectedAbilityIndex >= 0;
            using (new EditorGUI.DisabledScope(!hasSelection))
            {
                if (GUILayout.Button("Delete Selected", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                    DeleteSelectedWithConfirm();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                RefreshAssets();
        }
    }

    private void DrawEnemiesPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(620f)))
        {
            EditorGUILayout.LabelField("Enemies", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _enemiesFolder = EditorGUILayout.TextField("Enemies Folder", _enemiesFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newEnemyName = EditorGUILayout.TextField("New Enemy Name", _newEnemyName);
                    if (GUILayout.Button("Create", GUILayout.Width(110f), GUILayout.Height(24f)))
                        CreateEnemyAsset();
                }

                _enemySearch = EditorGUILayout.TextField("Search", _enemySearch);
            }

            EditorGUILayout.Space(6f);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
            for (int i = 0; i < _enemies.Count; i++)
            {
                EnemyData enemy = _enemies[i];
                if (enemy == null || !MatchesEnemySearch(enemy))
                    continue;

                DrawEnemyRow(i, enemy);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawAbilitiesPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(620f)))
        {
            EditorGUILayout.LabelField("Enemy Abilities", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _abilitiesFolder = EditorGUILayout.TextField("Abilities Folder", _abilitiesFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newAbilityName = EditorGUILayout.TextField("New Ability Name", _newAbilityName);
                    if (GUILayout.Button("Create", GUILayout.Width(110f), GUILayout.Height(24f)))
                        CreateAbilityAsset();
                }

                _abilitySearch = EditorGUILayout.TextField("Search", _abilitySearch);
            }

            EditorGUILayout.Space(6f);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
            for (int i = 0; i < _abilities.Count; i++)
            {
                EnemyAbilityData ability = _abilities[i];
                if (ability == null || !MatchesAbilitySearch(ability))
                    continue;

                DrawAbilityRow(i, ability);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawInspectorPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll, GUI.skin.box);
            if (_selectedAssetEditor != null)
                _selectedAssetEditor.OnInspectorGUI();
            else
                EditorGUILayout.HelpBox("Select an enemy or ability from the left list.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawEnemyRow(int index, EnemyData enemy)
    {
        bool selected = _selectedEnemyIndex == index;
        Rect rowRect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rowRect, selected ? new Color(0.24f, 0.45f, 0.85f, 0.45f) : new Color(0f, 0f, 0f, 0.08f));

        string assetPath = AssetDatabase.GetAssetPath(enemy);
        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, 220f, rowRect.height - 4f);
        Rect healthRect = new Rect(nameRect.xMax + 8f, rowRect.y + 2f, 70f, rowRect.height - 4f);
        Rect abilitiesRect = new Rect(healthRect.xMax + 8f, rowRect.y + 2f, 70f, rowRect.height - 4f);
        Rect pathRect = new Rect(abilitiesRect.xMax + 8f, rowRect.y + 2f, Mathf.Max(40f, rowRect.width - (abilitiesRect.xMax - rowRect.x) - 14f), rowRect.height - 4f);
        EditorGUI.LabelField(nameRect, enemy.name);
        EditorGUI.LabelField(healthRect, enemy.maxHealth.ToString());
        EditorGUI.LabelField(abilitiesRect, enemy.abilities != null ? enemy.abilities.Count.ToString() : "0");
        EditorGUI.LabelField(pathRect, assetPath);

        HandleRowEvents(rowRect, index, isEnemyRow: true);
    }

    private void DrawAbilityRow(int index, EnemyAbilityData ability)
    {
        bool selected = _selectedAbilityIndex == index;
        Rect rowRect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rowRect, selected ? new Color(0.24f, 0.45f, 0.85f, 0.45f) : new Color(0f, 0f, 0f, 0.08f));

        string displayName = string.IsNullOrWhiteSpace(ability.displayName) ? ability.name : ability.displayName;
        string assetPath = AssetDatabase.GetAssetPath(ability);
        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, 220f, rowRect.height - 4f);
        Rect weightRect = new Rect(nameRect.xMax + 8f, rowRect.y + 2f, 70f, rowRect.height - 4f);
        Rect effectsRect = new Rect(weightRect.xMax + 8f, rowRect.y + 2f, 70f, rowRect.height - 4f);
        Rect pathRect = new Rect(effectsRect.xMax + 8f, rowRect.y + 2f, Mathf.Max(40f, rowRect.width - (effectsRect.xMax - rowRect.x) - 14f), rowRect.height - 4f);
        EditorGUI.LabelField(nameRect, displayName);
        EditorGUI.LabelField(weightRect, ability.selectionWeight.ToString());
        EditorGUI.LabelField(effectsRect, ability.effects != null ? ability.effects.Count.ToString() : "0");
        EditorGUI.LabelField(pathRect, assetPath);

        HandleRowEvents(rowRect, index, isEnemyRow: false);
    }

    private void HandleRowEvents(Rect rowRect, int index, bool isEnemyRow)
    {
        Event currentEvent = Event.current;
        if (!rowRect.Contains(currentEvent.mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            if (isEnemyRow)
                SelectEnemy(index);
            else
                SelectAbility(index);

            currentEvent.Use();
            return;
        }

        if (currentEvent.type == EventType.ContextClick)
        {
            if (isEnemyRow)
                SelectEnemy(index);
            else
                SelectAbility(index);

            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedWithConfirm);
            menu.ShowAsContext();
            currentEvent.Use();
        }
    }

    private void CreateEnemyAsset()
    {
        EnsureFolderExists(_enemiesFolder);
        string safeName = string.IsNullOrWhiteSpace(_newEnemyName) ? "NewEnemyData" : _newEnemyName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_enemiesFolder}/{safeName}.asset");
        EnemyData asset = CreateInstance<EnemyData>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshAssets();
        SelectByPath(path, isEnemy: true);
    }

    private void CreateAbilityAsset()
    {
        EnsureFolderExists(_abilitiesFolder);
        string safeName = string.IsNullOrWhiteSpace(_newAbilityName) ? "NewEnemyAbility" : _newAbilityName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_abilitiesFolder}/{safeName}.asset");
        EnemyAbilityData asset = CreateInstance<EnemyAbilityData>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshAssets();
        SelectByPath(path, isEnemy: false);
    }

    private void RefreshAssets()
    {
        _enemies.Clear();
        _abilities.Clear();

        foreach (string guid in AssetDatabase.FindAssets("t:EnemyData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data != null)
                _enemies.Add(data);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:EnemyAbilityData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyAbilityData data = AssetDatabase.LoadAssetAtPath<EnemyAbilityData>(path);
            if (data != null)
                _abilities.Add(data);
        }

        _enemies.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _abilities.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _selectedEnemyIndex = -1;
        _selectedAbilityIndex = -1;
        SetSelectedAsset(null);
    }

    private bool MatchesEnemySearch(EnemyData enemy)
    {
        if (string.IsNullOrWhiteSpace(_enemySearch))
            return true;

        string query = _enemySearch.Trim();
        return ContainsIgnoreCase(enemy.name, query);
    }

    private bool MatchesAbilitySearch(EnemyAbilityData ability)
    {
        if (string.IsNullOrWhiteSpace(_abilitySearch))
            return true;

        string query = _abilitySearch.Trim();
        return ContainsIgnoreCase(ability.name, query)
               || ContainsIgnoreCase(ability.displayName, query)
               || ContainsIgnoreCase(ability.intentSummary, query);
    }

    private void SelectEnemy(int index)
    {
        if (index < 0 || index >= _enemies.Count)
            return;

        _selectedEnemyIndex = index;
        _selectedAbilityIndex = -1;
        SetSelectedAsset(_enemies[index]);
    }

    private void SelectAbility(int index)
    {
        if (index < 0 || index >= _abilities.Count)
            return;

        _selectedAbilityIndex = index;
        _selectedEnemyIndex = -1;
        SetSelectedAsset(_abilities[index]);
    }

    private void SetSelectedAsset(UnityEngine.Object asset)
    {
        if (_selectedAssetEditor != null)
            DestroyImmediate(_selectedAssetEditor);

        _selectedAssetEditor = asset != null ? Editor.CreateEditor(asset) : null;
        Selection.activeObject = asset;
        if (asset != null)
            EditorGUIUtility.PingObject(asset);
    }

    private void DeleteSelectedWithConfirm()
    {
        UnityEngine.Object selected = null;
        string title = "Delete Asset";
        string message = "Delete selected asset?";

        if (_selectedEnemyIndex >= 0 && _selectedEnemyIndex < _enemies.Count)
        {
            selected = _enemies[_selectedEnemyIndex];
            title = "Delete Enemy Data";
            message = $"Delete enemy '{selected.name}'?";
        }
        else if (_selectedAbilityIndex >= 0 && _selectedAbilityIndex < _abilities.Count)
        {
            selected = _abilities[_selectedAbilityIndex];
            title = "Delete Enemy Ability";
            message = $"Delete ability '{selected.name}'?";
        }

        if (selected == null || !EditorUtility.DisplayDialog(title, message, "Delete", "Cancel"))
            return;

        string path = AssetDatabase.GetAssetPath(selected);
        if (!string.IsNullOrWhiteSpace(path))
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        RefreshAssets();
    }

    private void SelectByPath(string path, bool isEnemy)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (isEnemy)
        {
            EnemyData created = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            int index = _enemies.IndexOf(created);
            if (index >= 0)
                SelectEnemy(index);
        }
        else
        {
            EnemyAbilityData created = AssetDatabase.LoadAssetAtPath<EnemyAbilityData>(path);
            int index = _abilities.IndexOf(created);
            if (index >= 0)
                SelectAbility(index);
        }

        _rightScroll = Vector2.zero;
        Repaint();
    }

    private static bool ContainsIgnoreCase(string source, string query)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(query))
            return false;

        return source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
