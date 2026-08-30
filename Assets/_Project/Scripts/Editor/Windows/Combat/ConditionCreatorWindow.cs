using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ConditionCreatorWindow : EditorWindow
{
    private const string DefaultConditionsFolder = "Assets/_Project/ScriptableObjects/Conditions";
    private const string DefaultEffectsFolder = "Assets/_Project/ScriptableObjects/Effects";
    private const string DefaultTargetsFolder = "Assets/_Project/ScriptableObjects/Targeting";
    private const string DefaultCoreDataFolder = "Assets/_Project/ScriptableObjects";

    private readonly List<BattleCondition> _conditions = new();
    private readonly List<Type> _conditionTypes = new();
    private readonly List<InstantBattleEffect> _effects = new();
    private readonly List<Type> _effectTypes = new();
    private readonly List<BattleTargetingProfile> _targets = new();
    private readonly List<Type> _targetTypes = new();
    private readonly List<ScriptableObject> _coreData = new();
    private readonly List<Type> _coreDataTypes = new();

    private string[] _conditionTypeNames = Array.Empty<string>();
    private string[] _effectTypeNames = Array.Empty<string>();
    private string[] _targetTypeNames = Array.Empty<string>();
    private readonly float[] _dataRowWidths = { 250f, 210f, 0f };
    private int _tabIndex;
    private int _conditionTypeIndex;
    private int _effectTypeIndex;
    private int _targetTypeIndex;
    private int _coreDataTypeIndex;
    private int _selectedConditionIndex = -1;
    private int _selectedEffectIndex = -1;
    private int _selectedTargetIndex = -1;
    private int _selectedCoreDataIndex = -1;

    private string _conditionsFolder = DefaultConditionsFolder;
    private string _effectsFolder = DefaultEffectsFolder;
    private string _targetsFolder = DefaultTargetsFolder;
    private string _newConditionName = "NewCondition";
    private string _newEffectName = "NewEffect";
    private string _newTargetName = "NewTargetProfile";
    private string _coreDataFolder = DefaultCoreDataFolder;
    private string _newCoreDataName = "NewDataAsset";
    private string _conditionSearch = string.Empty;
    private string _effectSearch = string.Empty;
    private string _targetSearch = string.Empty;
    private string _coreDataSearch = string.Empty;

    private Vector2 _leftScroll;
    private Vector2 _rightScroll;
    private Editor _selectedAssetEditor;

    [MenuItem("Tools/Combat Data/Database Editor")]
    public static void Open()
    {
        GetWindow<ConditionCreatorWindow>("Combat Data Database");
    }

    private void OnEnable()
    {
        BuildConditionTypeList();
        BuildEffectTypeList();
        BuildTargetTypeList();
        BuildCoreDataTypeList();
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
        _tabIndex = GUILayout.Toolbar(_tabIndex, new[] { "Conditions", "Instant Effects", "Targets", "Core Data" });
        EditorGUILayout.Space(6f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (_tabIndex == 0)
                DrawConditionsPanel();
            else if (_tabIndex == 1)
                DrawEffectsPanel();
            else if (_tabIndex == 2)
                DrawTargetsPanel();
            else
                DrawCoreDataPanel();
            DrawInspectorPanel();
        }
    }

    private void DrawTopBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Combat Data Tools", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();

            bool hasSelection = _selectedConditionIndex >= 0 || _selectedEffectIndex >= 0 || _selectedTargetIndex >= 0 || _selectedCoreDataIndex >= 0;
            using (new EditorGUI.DisabledScope(!hasSelection))
            {
                if (GUILayout.Button("Delete Selected", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                    DeleteSelectedWithConfirm();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                RefreshAssets();
        }
    }

    private void DrawConditionsPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(620f)))
        {
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _conditionsFolder = EditorGUILayout.TextField("Conditions Folder", _conditionsFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newConditionName = EditorGUILayout.TextField("New Condition Name", _newConditionName);
                    if (_conditionTypes.Count > 0)
                        _conditionTypeIndex = EditorGUILayout.Popup(_conditionTypeIndex, _conditionTypeNames, GUILayout.Width(220f));

                    using (new EditorGUI.DisabledScope(_conditionTypes.Count == 0))
                    {
                        if (GUILayout.Button("Create", GUILayout.Width(110f), GUILayout.Height(24f)))
                            CreateConditionAsset();
                    }
                }

                _conditionSearch = EditorGUILayout.TextField("Search", _conditionSearch);
            }

            EditorGUILayout.Space(6f);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
            EditorTableGUI.DrawHeader(new[] { "Name", "Type", "Asset" }, _dataRowWidths);
            for (int i = 0; i < _conditions.Count; i++)
            {
                BattleCondition condition = _conditions[i];
                if (condition == null || !MatchesConditionSearch(condition))
                    continue;

                DrawConditionRow(i, condition);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawEffectsPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(620f)))
        {
            EditorGUILayout.LabelField("Instant Effects", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _effectsFolder = EditorGUILayout.TextField("Instant Effects Folder", _effectsFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newEffectName = EditorGUILayout.TextField("New Effect Name", _newEffectName);
                    if (_effectTypes.Count > 0)
                        _effectTypeIndex = EditorGUILayout.Popup(_effectTypeIndex, _effectTypeNames, GUILayout.Width(220f));

                    using (new EditorGUI.DisabledScope(_effectTypes.Count == 0))
                    {
                        if (GUILayout.Button("Create", GUILayout.Width(110f), GUILayout.Height(24f)))
                            CreateEffectAsset();
                    }
                }

                _effectSearch = EditorGUILayout.TextField("Search", _effectSearch);
            }

            EditorGUILayout.Space(6f);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
            EditorTableGUI.DrawHeader(new[] { "Name", "Type", "Asset" }, _dataRowWidths);
            for (int i = 0; i < _effects.Count; i++)
            {
                InstantBattleEffect effect = _effects[i];
                if (effect == null || !MatchesEffectSearch(effect))
                    continue;

                DrawEffectRow(i, effect);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawTargetsPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(620f)))
        {
            EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _targetsFolder = EditorGUILayout.TextField("Targets Folder", _targetsFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newTargetName = EditorGUILayout.TextField("New Target Name", _newTargetName);
                    if (_targetTypes.Count > 0)
                        _targetTypeIndex = EditorGUILayout.Popup(_targetTypeIndex, _targetTypeNames, GUILayout.Width(220f));

                    using (new EditorGUI.DisabledScope(_targetTypes.Count == 0))
                    {
                        if (GUILayout.Button("Create", GUILayout.Width(110f), GUILayout.Height(24f)))
                            CreateTargetAsset();
                    }
                }

                _targetSearch = EditorGUILayout.TextField("Search", _targetSearch);
            }

            EditorGUILayout.Space(6f);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
            EditorTableGUI.DrawHeader(new[] { "Name", "Type", "Asset" }, _dataRowWidths);
            for (int i = 0; i < _targets.Count; i++)
            {
                BattleTargetingProfile target = _targets[i];
                if (target == null || !MatchesTargetSearch(target))
                    continue;

                DrawTargetRow(i, target);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawCoreDataPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(620f)))
        {
            EditorGUILayout.LabelField("Core Data", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _coreDataFolder = EditorGUILayout.TextField("Core Data Folder", _coreDataFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newCoreDataName = EditorGUILayout.TextField("New Asset Name", _newCoreDataName);
                    _coreDataTypeIndex = Mathf.Clamp(_coreDataTypeIndex, 0, Mathf.Max(0, _coreDataTypes.Count - 1));
                    if (_coreDataTypes.Count > 0)
                        _coreDataTypeIndex = EditorGUILayout.Popup(_coreDataTypeIndex, GetCoreDataTypeNames(), GUILayout.Width(220f));

                    using (new EditorGUI.DisabledScope(_coreDataTypes.Count == 0))
                    {
                        if (GUILayout.Button("Create", GUILayout.Width(110f), GUILayout.Height(24f)))
                            CreateCoreDataAsset();
                    }
                }

                _coreDataSearch = EditorGUILayout.TextField("Search", _coreDataSearch);
            }

            EditorGUILayout.Space(6f);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
            EditorTableGUI.DrawHeader(new[] { "Name", "Type", "Asset" }, _dataRowWidths);
            for (int i = 0; i < _coreData.Count; i++)
            {
                ScriptableObject data = _coreData[i];
                if (data == null || !MatchesCoreDataSearch(data))
                    continue;

                DrawCoreDataRow(i, data);
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
                EditorGUILayout.HelpBox("Select a condition, effect, or target profile from the left list.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawConditionRow(int index, BattleCondition condition)
    {
        string assetPath = AssetDatabase.GetAssetPath(condition);
        string conditionName = string.IsNullOrWhiteSpace(condition.DisplayName) ? condition.name : condition.DisplayName;
        EditorTableGUI.DrawRow(
            new[] { conditionName, condition.GetType().Name, assetPath },
            _dataRowWidths,
            _selectedConditionIndex == index,
            () => SelectCondition(index),
            ShowDeleteContextMenu);
    }

    private void DrawEffectRow(int index, InstantBattleEffect effect)
    {
        string assetPath = AssetDatabase.GetAssetPath(effect);
        string effectName = string.IsNullOrWhiteSpace(effect.EffectName) ? effect.name : effect.EffectName;
        EditorTableGUI.DrawRow(
            new[] { effectName, effect.GetType().Name, assetPath },
            _dataRowWidths,
            _selectedEffectIndex == index,
            () => SelectEffect(index),
            ShowDeleteContextMenu);
    }

    private void DrawTargetRow(int index, BattleTargetingProfile target)
    {
        string assetPath = AssetDatabase.GetAssetPath(target);
        string targetName = string.IsNullOrWhiteSpace(target.DisplayName) ? target.name : target.DisplayName;
        EditorTableGUI.DrawRow(
            new[] { targetName, target.GetType().Name, assetPath },
            _dataRowWidths,
            _selectedTargetIndex == index,
            () => SelectTarget(index),
            ShowDeleteContextMenu);
    }

    private void DrawCoreDataRow(int index, ScriptableObject data)
    {
        EditorTableGUI.DrawRow(
            new[] { data.name, data.GetType().Name, AssetDatabase.GetAssetPath(data) },
            _dataRowWidths,
            _selectedCoreDataIndex == index,
            () => SelectCoreData(index),
            ShowDeleteContextMenu);
    }

    private void ShowDeleteContextMenu()
    {
        GenericMenu menu = new();
        menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedWithConfirm);
        menu.ShowAsContext();
    }

    private void BuildConditionTypeList()
    {
        _conditionTypes.Clear();
        _conditionTypes.AddRange(EditorAssetUtility.LoadConcreteTypes<BattleCondition>());
        _conditionTypeNames = _conditionTypes.Select(type => type.Name).ToArray();
        _conditionTypeIndex = Mathf.Clamp(_conditionTypeIndex, 0, Mathf.Max(0, _conditionTypes.Count - 1));
    }

    private void BuildEffectTypeList()
    {
        _effectTypes.Clear();
        _effectTypes.AddRange(EditorAssetUtility.LoadConcreteTypes<InstantBattleEffect>());
        _effectTypeNames = _effectTypes.Select(type => type.Name).ToArray();
        _effectTypeIndex = Mathf.Clamp(_effectTypeIndex, 0, Mathf.Max(0, _effectTypes.Count - 1));
    }

    private void BuildTargetTypeList()
    {
        _targetTypes.Clear();
        _targetTypes.AddRange(EditorAssetUtility.LoadConcreteTypes<BattleTargetingProfile>());
        _targetTypeNames = _targetTypes.Select(type => type.Name).ToArray();
        _targetTypeIndex = Mathf.Clamp(_targetTypeIndex, 0, Mathf.Max(0, _targetTypes.Count - 1));
    }

    private void BuildCoreDataTypeList()
    {
        _coreDataTypes.Clear();
        _coreDataTypes.Add(typeof(PlayerData));
        _coreDataTypes.Add(typeof(DeckData));
        _coreDataTypes.Add(typeof(InventoryData));
        _coreDataTypeIndex = Mathf.Clamp(_coreDataTypeIndex, 0, Mathf.Max(0, _coreDataTypes.Count - 1));
    }

    private void CreateConditionAsset()
    {
        Type conditionType = _conditionTypes[Mathf.Clamp(_conditionTypeIndex, 0, _conditionTypes.Count - 1)];
        BattleCondition asset = EditorAssetUtility.CreateAsset(
            conditionType,
            _conditionsFolder,
            GetAssetNameOrFallback(_newConditionName, "NewCondition")) as BattleCondition;
        RefreshAssets();
        SelectAsset(asset);
    }

    private void CreateEffectAsset()
    {
        Type effectType = _effectTypes[Mathf.Clamp(_effectTypeIndex, 0, _effectTypes.Count - 1)];
        InstantBattleEffect asset = EditorAssetUtility.CreateAsset(
            effectType,
            _effectsFolder,
            GetAssetNameOrFallback(_newEffectName, "NewEffect")) as InstantBattleEffect;
        RefreshAssets();
        SelectAsset(asset);
    }

    private void CreateTargetAsset()
    {
        Type targetType = _targetTypes[Mathf.Clamp(_targetTypeIndex, 0, _targetTypes.Count - 1)];
        BattleTargetingProfile asset = EditorAssetUtility.CreateAsset(
            targetType,
            _targetsFolder,
            GetAssetNameOrFallback(_newTargetName, "NewTargetProfile")) as BattleTargetingProfile;
        RefreshAssets();
        SelectAsset(asset);
    }

    private void CreateCoreDataAsset()
    {
        if (_coreDataTypes.Count == 0)
            return;

        Type dataType = _coreDataTypes[Mathf.Clamp(_coreDataTypeIndex, 0, _coreDataTypes.Count - 1)];
        ScriptableObject asset = EditorAssetUtility.CreateAsset(
            dataType,
            _coreDataFolder,
            GetAssetNameOrFallback(_newCoreDataName, "NewDataAsset"));
        RefreshAssets();
        SelectAsset(asset);
    }

    private void RefreshAssets()
    {
        _conditions.Clear();
        _effects.Clear();
        _targets.Clear();
        _coreData.Clear();
        _conditions.AddRange(EditorAssetUtility.LoadAssets<BattleCondition>());
        _targets.AddRange(EditorAssetUtility.LoadAssets<BattleTargetingProfile>());

        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            InstantBattleEffect effect = AssetDatabase.LoadAssetAtPath<InstantBattleEffect>(path);
            if (effect != null)
                _effects.Add(effect);
        }

        LoadCoreDataAssets<PlayerData>();
        LoadCoreDataAssets<DeckData>();
        LoadCoreDataAssets<InventoryData>();

        _effects.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _coreData.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _selectedConditionIndex = -1;
        _selectedEffectIndex = -1;
        _selectedTargetIndex = -1;
        _selectedCoreDataIndex = -1;
        SetSelectedAsset(null);
    }

    private void LoadCoreDataAssets<T>() where T : ScriptableObject
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T data = AssetDatabase.LoadAssetAtPath<T>(path);
            if (data != null)
                _coreData.Add(data);
        }
    }

    private bool MatchesConditionSearch(BattleCondition condition)
    {
        if (string.IsNullOrWhiteSpace(_conditionSearch))
            return true;

        string query = _conditionSearch.Trim();
        return EditorAssetUtility.ContainsIgnoreCase(condition.name, query)
               || EditorAssetUtility.ContainsIgnoreCase(condition.ConditionID, query)
               || EditorAssetUtility.ContainsIgnoreCase(condition.DisplayName, query)
               || EditorAssetUtility.ContainsIgnoreCase(condition.Description, query)
               || EditorAssetUtility.ContainsIgnoreCase(condition.GetType().Name, query);
    }

    private bool MatchesEffectSearch(InstantBattleEffect effect)
    {
        if (string.IsNullOrWhiteSpace(_effectSearch))
            return true;

        string query = _effectSearch.Trim();
        return EditorAssetUtility.ContainsIgnoreCase(effect.name, query)
               || EditorAssetUtility.ContainsIgnoreCase(effect.EffectID, query)
               || EditorAssetUtility.ContainsIgnoreCase(effect.EffectName, query)
               || EditorAssetUtility.ContainsIgnoreCase(effect.Description, query)
               || EditorAssetUtility.ContainsIgnoreCase(effect.GetType().Name, query);
    }

    private bool MatchesTargetSearch(BattleTargetingProfile target)
    {
        if (string.IsNullOrWhiteSpace(_targetSearch))
            return true;

        string query = _targetSearch.Trim();
        return EditorAssetUtility.ContainsIgnoreCase(target.name, query)
               || EditorAssetUtility.ContainsIgnoreCase(target.TargetingID, query)
               || EditorAssetUtility.ContainsIgnoreCase(target.DisplayName, query)
               || EditorAssetUtility.ContainsIgnoreCase(target.Description, query)
               || EditorAssetUtility.ContainsIgnoreCase(target.GetType().Name, query);
    }

    private bool MatchesCoreDataSearch(ScriptableObject data)
    {
        if (string.IsNullOrWhiteSpace(_coreDataSearch))
            return true;

        string query = _coreDataSearch.Trim();
        return EditorAssetUtility.ContainsIgnoreCase(data.name, query)
               || EditorAssetUtility.ContainsIgnoreCase(data.GetType().Name, query);
    }

    private void SelectCondition(int index)
    {
        if (index < 0 || index >= _conditions.Count)
            return;

        _selectedConditionIndex = index;
        _selectedEffectIndex = -1;
        _selectedTargetIndex = -1;
        _selectedCoreDataIndex = -1;
        SetSelectedAsset(_conditions[index]);
    }

    private void SelectEffect(int index)
    {
        if (index < 0 || index >= _effects.Count)
            return;

        _selectedEffectIndex = index;
        _selectedConditionIndex = -1;
        _selectedTargetIndex = -1;
        _selectedCoreDataIndex = -1;
        SetSelectedAsset(_effects[index]);
    }

    private void SelectTarget(int index)
    {
        if (index < 0 || index >= _targets.Count)
            return;

        _selectedTargetIndex = index;
        _selectedConditionIndex = -1;
        _selectedEffectIndex = -1;
        _selectedCoreDataIndex = -1;
        SetSelectedAsset(_targets[index]);
    }

    private void SelectCoreData(int index)
    {
        if (index < 0 || index >= _coreData.Count)
            return;

        _selectedCoreDataIndex = index;
        _selectedConditionIndex = -1;
        _selectedEffectIndex = -1;
        _selectedTargetIndex = -1;
        SetSelectedAsset(_coreData[index]);
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

        if (_selectedConditionIndex >= 0 && _selectedConditionIndex < _conditions.Count)
        {
            selected = _conditions[_selectedConditionIndex];
            title = "Delete Condition";
            message = $"Delete condition '{selected.name}'?";
        }
        else if (_selectedEffectIndex >= 0 && _selectedEffectIndex < _effects.Count)
        {
            selected = _effects[_selectedEffectIndex];
            title = "Delete Effect";
            message = $"Delete effect '{selected.name}'?";
        }
        else if (_selectedTargetIndex >= 0 && _selectedTargetIndex < _targets.Count)
        {
            selected = _targets[_selectedTargetIndex];
            title = "Delete Target Profile";
            message = $"Delete target profile '{selected.name}'?";
        }
        else if (_selectedCoreDataIndex >= 0 && _selectedCoreDataIndex < _coreData.Count)
        {
            selected = _coreData[_selectedCoreDataIndex];
            title = $"Delete {selected.GetType().Name}";
            message = $"Delete '{selected.name}'?";
        }

        if (selected == null)
            return;
        if (!EditorUtility.DisplayDialog(title, message, "Delete", "Cancel"))
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

        if (asset is BattleCondition condition)
        {
            int index = _conditions.IndexOf(condition);
            if (index >= 0)
                SelectCondition(index);
        }
        else if (asset is InstantBattleEffect effect)
        {
            int index = _effects.IndexOf(effect);
            if (index >= 0)
                SelectEffect(index);
        }
        else if (asset is BattleTargetingProfile target)
        {
            int index = _targets.IndexOf(target);
            if (index >= 0)
                SelectTarget(index);
        }
        else if (asset is ScriptableObject data)
        {
            int index = _coreData.IndexOf(data);
            if (index >= 0)
                SelectCoreData(index);
        }

        _rightScroll = Vector2.zero;
        Repaint();
    }

    private string[] GetCoreDataTypeNames()
    {
        return _coreDataTypes.Select(type => type.Name).ToArray();
    }

    private static string GetAssetNameOrFallback(string assetName, string fallback)
    {
        return string.IsNullOrWhiteSpace(assetName) ? fallback : assetName.Trim();
    }
}
