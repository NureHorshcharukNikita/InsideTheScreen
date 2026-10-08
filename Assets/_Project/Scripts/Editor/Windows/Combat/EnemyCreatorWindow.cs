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
    private readonly float[] _enemyRowWidths = { 220f, 70f, 70f, 0f };

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
            EditorTableGUI.DrawHeader(new[] { "Name", "Health", "Abilities", "Asset" }, _enemyRowWidths);
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
            EditorTableGUI.DrawHeader(new[] { "Name", "Weight", "Effects", "Asset" }, _enemyRowWidths);
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
            if (_selectedAssetEditor != null && _selectedAssetEditor.target != null)
                _selectedAssetEditor.OnInspectorGUI();
            else
                EditorGUILayout.HelpBox("Select an enemy or ability from the left list.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawEnemyRow(int index, EnemyData enemy)
    {
        EditorTableGUI.DrawRow(
            new[]
            {
                enemy.DisplayName,
                enemy.maxHealth.ToString(),
                enemy.abilities != null ? enemy.abilities.Count.ToString() : "0",
                AssetDatabase.GetAssetPath(enemy)
            },
            _enemyRowWidths,
            _selectedEnemyIndex == index,
            () => SelectEnemy(index),
            ShowDeleteContextMenu);
    }

    private void DrawAbilityRow(int index, EnemyAbilityData ability)
    {
        string displayName = string.IsNullOrWhiteSpace(ability.displayName) ? ability.name : ability.displayName;
        int instantEffectCount = ability.instantEffects != null ? ability.instantEffects.Count : 0;
        int statusApplicationCount = ability.statusApplications != null ? ability.statusApplications.Count : 0;
        EditorTableGUI.DrawRow(
            new[]
            {
                displayName,
                ability.selectionWeight.ToString(),
                (instantEffectCount + statusApplicationCount).ToString(),
                AssetDatabase.GetAssetPath(ability)
            },
            _enemyRowWidths,
            _selectedAbilityIndex == index,
            () => SelectAbility(index),
            ShowDeleteContextMenu);
    }

    private void ShowDeleteContextMenu()
    {
        GenericMenu menu = new();
        menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedWithConfirm);
        menu.ShowAsContext();
    }

    private void CreateEnemyAsset()
    {
        EnemyData asset = EditorAssetUtility.CreateAsset<EnemyData>(
            _enemiesFolder,
            GetAssetNameOrFallback(_newEnemyName, "NewEnemyData"));
        RefreshAssets();
        SelectAsset(asset);
    }

    private void CreateAbilityAsset()
    {
        EnemyAbilityData asset = EditorAssetUtility.CreateAsset<EnemyAbilityData>(
            _abilitiesFolder,
            GetAssetNameOrFallback(_newAbilityName, "NewEnemyAbility"));
        RefreshAssets();
        SelectAsset(asset);
    }

    private void RefreshAssets()
    {
        _enemies.Clear();
        _abilities.Clear();

        _enemies.AddRange(EditorAssetUtility.LoadAssets<EnemyData>());
        _abilities.AddRange(EditorAssetUtility.LoadAssets<EnemyAbilityData>());
        _selectedEnemyIndex = -1;
        _selectedAbilityIndex = -1;
        SetSelectedAsset(null);
    }

    private bool MatchesEnemySearch(EnemyData enemy)
    {
        if (string.IsNullOrWhiteSpace(_enemySearch))
            return true;

        string query = _enemySearch.Trim();
        return EditorAssetUtility.ContainsIgnoreCase(enemy.name, query)
               || EditorAssetUtility.ContainsIgnoreCase(enemy.CharacterID, query)
               || EditorAssetUtility.ContainsIgnoreCase(enemy.DisplayName, query)
               || EditorAssetUtility.ContainsIgnoreCase(enemy.Description, query);
    }

    private bool MatchesAbilitySearch(EnemyAbilityData ability)
    {
        if (string.IsNullOrWhiteSpace(_abilitySearch))
            return true;

        string query = _abilitySearch.Trim();
        return EditorAssetUtility.ContainsIgnoreCase(ability.name, query)
               || EditorAssetUtility.ContainsIgnoreCase(ability.displayName, query)
               || EditorAssetUtility.ContainsIgnoreCase(ability.intentSummary, query);
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

    private void SelectAsset(UnityEngine.Object asset)
    {
        if (asset == null)
            return;

        if (asset is EnemyData enemy)
        {
            int index = _enemies.IndexOf(enemy);
            if (index >= 0)
                SelectEnemy(index);
        }
        else if (asset is EnemyAbilityData ability)
        {
            int index = _abilities.IndexOf(ability);
            if (index >= 0)
                SelectAbility(index);
        }

        _rightScroll = Vector2.zero;
        Repaint();
    }

    private static string GetAssetNameOrFallback(string assetName, string fallback)
    {
        return string.IsNullOrWhiteSpace(assetName) ? fallback : assetName.Trim();
    }
}
