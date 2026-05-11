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
    private readonly List<BattleEffect> _effects = new();
    private readonly List<Type> _effectTypes = new();
    private readonly List<BattleTargetingProfile> _targets = new();
    private readonly List<Type> _targetTypes = new();
    private readonly List<ScriptableObject> _coreData = new();
    private readonly List<Type> _coreDataTypes = new();

    private string[] _conditionTypeNames = Array.Empty<string>();
    private string[] _effectTypeNames = Array.Empty<string>();
    private string[] _targetTypeNames = Array.Empty<string>();
    private string[] _coreDataTypeNames = Array.Empty<string>();
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
        _tabIndex = GUILayout.Toolbar(_tabIndex, new[] { "Conditions", "Effects", "Targets", "Core Data" });
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
            EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _effectsFolder = EditorGUILayout.TextField("Effects Folder", _effectsFolder);
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
            for (int i = 0; i < _effects.Count; i++)
            {
                BattleEffect effect = _effects[i];
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
                    if (_coreDataTypes.Count > 0)
                        _coreDataTypeIndex = EditorGUILayout.Popup(_coreDataTypeIndex, _coreDataTypeNames, GUILayout.Width(220f));

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
        bool selected = _selectedConditionIndex == index;
        Rect rowRect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rowRect, selected ? new Color(0.24f, 0.45f, 0.85f, 0.45f) : new Color(0f, 0f, 0f, 0.08f));

        string assetPath = AssetDatabase.GetAssetPath(condition);
        string conditionName = string.IsNullOrWhiteSpace(condition.DisplayName) ? condition.name : condition.DisplayName;
        string typeName = condition.GetType().Name;
        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, 250f, rowRect.height - 4f);
        Rect typeRect = new Rect(nameRect.xMax + 8f, rowRect.y + 2f, 210f, rowRect.height - 4f);
        Rect pathRect = new Rect(typeRect.xMax + 8f, rowRect.y + 2f, Mathf.Max(40f, rowRect.width - (typeRect.xMax - rowRect.x) - 14f), rowRect.height - 4f);
        EditorGUI.LabelField(nameRect, conditionName);
        EditorGUI.LabelField(typeRect, typeName);
        EditorGUI.LabelField(pathRect, assetPath);

        HandleConditionRowEvents(rowRect, index);
    }

    private void DrawEffectRow(int index, BattleEffect effect)
    {
        bool selected = _selectedEffectIndex == index;
        Rect rowRect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rowRect, selected ? new Color(0.24f, 0.45f, 0.85f, 0.45f) : new Color(0f, 0f, 0f, 0.08f));

        string assetPath = AssetDatabase.GetAssetPath(effect);
        string effectName = string.IsNullOrWhiteSpace(effect.EffectName) ? effect.name : effect.EffectName;
        string typeName = effect.GetType().Name;
        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, 250f, rowRect.height - 4f);
        Rect typeRect = new Rect(nameRect.xMax + 8f, rowRect.y + 2f, 210f, rowRect.height - 4f);
        Rect pathRect = new Rect(typeRect.xMax + 8f, rowRect.y + 2f, Mathf.Max(40f, rowRect.width - (typeRect.xMax - rowRect.x) - 14f), rowRect.height - 4f);
        EditorGUI.LabelField(nameRect, effectName);
        EditorGUI.LabelField(typeRect, typeName);
        EditorGUI.LabelField(pathRect, assetPath);

        HandleEffectRowEvents(rowRect, index);
    }

    private void DrawTargetRow(int index, BattleTargetingProfile target)
    {
        bool selected = _selectedTargetIndex == index;
        Rect rowRect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rowRect, selected ? new Color(0.24f, 0.45f, 0.85f, 0.45f) : new Color(0f, 0f, 0f, 0.08f));

        string assetPath = AssetDatabase.GetAssetPath(target);
        string targetName = string.IsNullOrWhiteSpace(target.DisplayName) ? target.name : target.DisplayName;
        string typeName = target.GetType().Name;
        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, 250f, rowRect.height - 4f);
        Rect typeRect = new Rect(nameRect.xMax + 8f, rowRect.y + 2f, 210f, rowRect.height - 4f);
        Rect pathRect = new Rect(typeRect.xMax + 8f, rowRect.y + 2f, Mathf.Max(40f, rowRect.width - (typeRect.xMax - rowRect.x) - 14f), rowRect.height - 4f);
        EditorGUI.LabelField(nameRect, targetName);
        EditorGUI.LabelField(typeRect, typeName);
        EditorGUI.LabelField(pathRect, assetPath);

        HandleTargetRowEvents(rowRect, index);
    }

    private void DrawCoreDataRow(int index, ScriptableObject data)
    {
        bool selected = _selectedCoreDataIndex == index;
        Rect rowRect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rowRect, selected ? new Color(0.24f, 0.45f, 0.85f, 0.45f) : new Color(0f, 0f, 0f, 0.08f));

        string assetPath = AssetDatabase.GetAssetPath(data);
        string typeName = data.GetType().Name;
        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, 250f, rowRect.height - 4f);
        Rect typeRect = new Rect(nameRect.xMax + 8f, rowRect.y + 2f, 210f, rowRect.height - 4f);
        Rect pathRect = new Rect(typeRect.xMax + 8f, rowRect.y + 2f, Mathf.Max(40f, rowRect.width - (typeRect.xMax - rowRect.x) - 14f), rowRect.height - 4f);
        EditorGUI.LabelField(nameRect, data.name);
        EditorGUI.LabelField(typeRect, typeName);
        EditorGUI.LabelField(pathRect, assetPath);

        HandleCoreDataRowEvents(rowRect, index);
    }

    private void HandleConditionRowEvents(Rect rowRect, int index)
    {
        Event currentEvent = Event.current;
        if (!rowRect.Contains(currentEvent.mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            SelectCondition(index);
            currentEvent.Use();
            return;
        }

        if (currentEvent.type == EventType.ContextClick)
        {
            SelectCondition(index);
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedWithConfirm);
            menu.ShowAsContext();
            currentEvent.Use();
        }
    }

    private void HandleEffectRowEvents(Rect rowRect, int index)
    {
        Event currentEvent = Event.current;
        if (!rowRect.Contains(currentEvent.mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            SelectEffect(index);
            currentEvent.Use();
            return;
        }

        if (currentEvent.type == EventType.ContextClick)
        {
            SelectEffect(index);
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedWithConfirm);
            menu.ShowAsContext();
            currentEvent.Use();
        }
    }

    private void HandleTargetRowEvents(Rect rowRect, int index)
    {
        Event currentEvent = Event.current;
        if (!rowRect.Contains(currentEvent.mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            SelectTarget(index);
            currentEvent.Use();
            return;
        }

        if (currentEvent.type == EventType.ContextClick)
        {
            SelectTarget(index);
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedWithConfirm);
            menu.ShowAsContext();
            currentEvent.Use();
        }
    }

    private void HandleCoreDataRowEvents(Rect rowRect, int index)
    {
        Event currentEvent = Event.current;
        if (!rowRect.Contains(currentEvent.mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            SelectCoreData(index);
            currentEvent.Use();
            return;
        }

        if (currentEvent.type == EventType.ContextClick)
        {
            SelectCoreData(index);
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedWithConfirm);
            menu.ShowAsContext();
            currentEvent.Use();
        }
    }

    private void BuildConditionTypeList()
    {
        _conditionTypes.Clear();
        foreach (Type type in TypeCache.GetTypesDerivedFrom<BattleCondition>())
        {
            if (!type.IsAbstract)
                _conditionTypes.Add(type);
        }

        _conditionTypes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        _conditionTypeNames = _conditionTypes.Select(type => type.Name).ToArray();
        _conditionTypeIndex = Mathf.Clamp(_conditionTypeIndex, 0, Mathf.Max(0, _conditionTypes.Count - 1));
    }

    private void BuildEffectTypeList()
    {
        _effectTypes.Clear();
        foreach (Type type in TypeCache.GetTypesDerivedFrom<BattleEffect>())
        {
            if (!type.IsAbstract)
                _effectTypes.Add(type);
        }

        _effectTypes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        _effectTypeNames = _effectTypes.Select(type => type.Name).ToArray();
        _effectTypeIndex = Mathf.Clamp(_effectTypeIndex, 0, Mathf.Max(0, _effectTypes.Count - 1));
    }

    private void BuildTargetTypeList()
    {
        _targetTypes.Clear();
        foreach (Type type in TypeCache.GetTypesDerivedFrom<BattleTargetingProfile>())
        {
            if (!type.IsAbstract)
                _targetTypes.Add(type);
        }

        _targetTypes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        _targetTypeNames = _targetTypes.Select(type => type.Name).ToArray();
        _targetTypeIndex = Mathf.Clamp(_targetTypeIndex, 0, Mathf.Max(0, _targetTypes.Count - 1));
    }

    private void BuildCoreDataTypeList()
    {
        _coreDataTypes.Clear();
        _coreDataTypes.Add(typeof(PlayerData));
        _coreDataTypes.Add(typeof(DeckData));
        _coreDataTypes.Add(typeof(InventoryData));

        _coreDataTypeNames = _coreDataTypes.Select(type => type.Name).ToArray();
        _coreDataTypeIndex = Mathf.Clamp(_coreDataTypeIndex, 0, Mathf.Max(0, _coreDataTypes.Count - 1));
    }

    private void CreateConditionAsset()
    {
        EnsureFolderExists(_conditionsFolder);
        string safeName = string.IsNullOrWhiteSpace(_newConditionName) ? "NewCondition" : _newConditionName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_conditionsFolder}/{safeName}.asset");
        Type conditionType = _conditionTypes[Mathf.Clamp(_conditionTypeIndex, 0, _conditionTypes.Count - 1)];
        BattleCondition asset = (BattleCondition)CreateInstance(conditionType);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshAssets();
        SelectByPath(path);
    }

    private void CreateEffectAsset()
    {
        EnsureFolderExists(_effectsFolder);
        string safeName = string.IsNullOrWhiteSpace(_newEffectName) ? "NewEffect" : _newEffectName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_effectsFolder}/{safeName}.asset");
        Type effectType = _effectTypes[Mathf.Clamp(_effectTypeIndex, 0, _effectTypes.Count - 1)];
        BattleEffect asset = (BattleEffect)CreateInstance(effectType);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshAssets();
        SelectByPath(path, isCondition: false);
    }

    private void CreateTargetAsset()
    {
        EnsureFolderExists(_targetsFolder);
        string safeName = string.IsNullOrWhiteSpace(_newTargetName) ? "NewTargetProfile" : _newTargetName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_targetsFolder}/{safeName}.asset");
        Type targetType = _targetTypes[Mathf.Clamp(_targetTypeIndex, 0, _targetTypes.Count - 1)];
        BattleTargetingProfile asset = (BattleTargetingProfile)CreateInstance(targetType);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshAssets();
        SelectByPath(path, isCondition: false, isEffect: false);
    }

    private void CreateCoreDataAsset()
    {
        EnsureFolderExists(_coreDataFolder);
        string safeName = string.IsNullOrWhiteSpace(_newCoreDataName) ? "NewDataAsset" : _newCoreDataName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_coreDataFolder}/{safeName}.asset");
        Type dataType = _coreDataTypes[Mathf.Clamp(_coreDataTypeIndex, 0, _coreDataTypes.Count - 1)];
        ScriptableObject asset = CreateInstance(dataType);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshAssets();
        SelectByPath(path, isCondition: false, isEffect: false, isTarget: false);
    }

    private void RefreshAssets()
    {
        _conditions.Clear();
        _effects.Clear();
        _targets.Clear();
        _coreData.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:BattleCondition"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BattleCondition condition = AssetDatabase.LoadAssetAtPath<BattleCondition>(path);
            if (condition != null)
                _conditions.Add(condition);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BattleEffect effect = AssetDatabase.LoadAssetAtPath<BattleEffect>(path);
            if (effect != null)
                _effects.Add(effect);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:BattleTargetingProfile"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BattleTargetingProfile target = AssetDatabase.LoadAssetAtPath<BattleTargetingProfile>(path);
            if (target != null)
                _targets.Add(target);
        }

        LoadCoreDataAssets<PlayerData>();
        LoadCoreDataAssets<DeckData>();
        LoadCoreDataAssets<InventoryData>();

        _conditions.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _effects.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _targets.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
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
        return ContainsIgnoreCase(condition.name, query)
               || ContainsIgnoreCase(condition.ConditionID, query)
               || ContainsIgnoreCase(condition.DisplayName, query)
               || ContainsIgnoreCase(condition.Description, query)
               || ContainsIgnoreCase(condition.GetType().Name, query);
    }

    private bool MatchesEffectSearch(BattleEffect effect)
    {
        if (string.IsNullOrWhiteSpace(_effectSearch))
            return true;

        string query = _effectSearch.Trim();
        return ContainsIgnoreCase(effect.name, query)
               || ContainsIgnoreCase(effect.EffectID, query)
               || ContainsIgnoreCase(effect.EffectName, query)
               || ContainsIgnoreCase(effect.Description, query)
               || ContainsIgnoreCase(effect.GetType().Name, query);
    }

    private bool MatchesTargetSearch(BattleTargetingProfile target)
    {
        if (string.IsNullOrWhiteSpace(_targetSearch))
            return true;

        string query = _targetSearch.Trim();
        return ContainsIgnoreCase(target.name, query)
               || ContainsIgnoreCase(target.TargetingID, query)
               || ContainsIgnoreCase(target.DisplayName, query)
               || ContainsIgnoreCase(target.Description, query)
               || ContainsIgnoreCase(target.GetType().Name, query);
    }

    private bool MatchesCoreDataSearch(ScriptableObject data)
    {
        if (string.IsNullOrWhiteSpace(_coreDataSearch))
            return true;

        string query = _coreDataSearch.Trim();
        return ContainsIgnoreCase(data.name, query)
               || ContainsIgnoreCase(data.GetType().Name, query);
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

    private void SelectByPath(string path, bool isCondition = true, bool isEffect = true, bool isTarget = true)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (isCondition)
        {
            BattleCondition created = AssetDatabase.LoadAssetAtPath<BattleCondition>(path);
            int index = _conditions.IndexOf(created);
            if (index >= 0)
                SelectCondition(index);
        }
        else if (isEffect)
        {
            BattleEffect created = AssetDatabase.LoadAssetAtPath<BattleEffect>(path);
            int index = _effects.IndexOf(created);
            if (index >= 0)
                SelectEffect(index);
        }
        else if (isTarget)
        {
            BattleTargetingProfile created = AssetDatabase.LoadAssetAtPath<BattleTargetingProfile>(path);
            int index = _targets.IndexOf(created);
            if (index >= 0)
                SelectTarget(index);
        }
        else
        {
            ScriptableObject created = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            int index = _coreData.IndexOf(created);
            if (index >= 0)
                SelectCoreData(index);
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
