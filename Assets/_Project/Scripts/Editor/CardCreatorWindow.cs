using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class CardCreatorWindow : EditorWindow
{
    private const string DefaultCardsFolder = "Assets/_Project/ScriptableObjects/Cards";
    private const string DefaultEffectsFolder = "Assets/_Project/ScriptableObjects/Effects";
    private const string DefaultStatusesFolder = "Assets/_Project/ScriptableObjects/Effects/Statuses";
    private const string DefaultDisplaysFolder = "Assets/_Project/ScriptableObjects/Effects/Display";

    private readonly List<CardData> _cards = new();
    private readonly List<InstantBattleEffect> _effects = new();
    private readonly List<BattleStatus> _statuses = new();
    private readonly List<BattleEffectDisplayData> _displays = new();
    private readonly List<Type> _effectTypes = new();
    private readonly List<Type> _statusTypes = new();

    private string[] _effectTypeNames = Array.Empty<string>();
    private string[] _statusTypeNames = Array.Empty<string>();
    private int _effectTypeIndex;
    private int _statusTypeIndex;
    private int _tabIndex;
    private int _selectedCardIndex = -1;
    private int _selectedEffectIndex = -1;
    private int _selectedStatusIndex = -1;
    private int _selectedDisplayIndex = -1;

    private string _cardsFolder = DefaultCardsFolder;
    private string _effectsFolder = DefaultEffectsFolder;
    private string _statusesFolder = DefaultStatusesFolder;
    private string _displaysFolder = DefaultDisplaysFolder;
    private string _newCardName = "NewCard";
    private string _newEffectName = "NewEffect";
    private string _newStatusName = "NewStatus";
    private string _newDisplayName = "NewDisplay";
    private string _cardSearch = "";
    private string _effectSearch = "";
    private string _statusSearch = "";
    private string _displaySearch = "";

    private Vector2 _leftScroll;
    private Vector2 _rightScroll;
    private Editor _selectedAssetEditor;

    private bool _isResizingColumn;
    private string _resizingColumnKey;
    private Vector2 _resizeMouseStart;
    private float _resizeWidthStart;

    private float _cardColName = 180f;
    private float _cardColId = 140f;
    private float _cardColCost = 60f;
    private float _cardColEffects = 70f;
    private float _effectColName = 220f;
    private float _effectColType = 160f;

    [MenuItem("Tools/Cards/Card Database Editor")]
    public static void Open()
    {
        GetWindow<CardCreatorWindow>("Card Database");
    }

    private void OnEnable()
    {
        BuildEffectTypeList();
        BuildStatusTypeList();
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
        _tabIndex = GUILayout.Toolbar(_tabIndex, new[] { "Cards", "Instant Effects", "Battle Statuses", "Effect Displays" });
        EditorGUILayout.Space(6);

        if (_tabIndex == 0)
            DrawCardsTab();
        else if (_tabIndex == 1)
            DrawEffectsTab();
        else if (_tabIndex == 2)
            DrawStatusesTab();
        else
            DrawDisplaysTab();
    }

    private void DrawTopBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Card Tools", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            bool hasSelection = _selectedCardIndex >= 0 || _selectedEffectIndex >= 0 || _selectedStatusIndex >= 0 || _selectedDisplayIndex >= 0;
            using (new EditorGUI.DisabledScope(!hasSelection))
            {
                if (GUILayout.Button("Delete Selected", EditorStyles.toolbarButton, GUILayout.Width(100)))
                    DeleteSelectedAssetWithConfirm();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshAssets();
        }
    }

    private void DrawCardsTab()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawCardsListPanel();
            DrawSelectedInspectorPanel();
        }
    }

    private void DrawEffectsTab()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawEffectsListPanel();
            DrawSelectedInspectorPanel();
        }
    }

    private void DrawStatusesTab()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawStatusesListPanel();
            DrawSelectedInspectorPanel();
        }
    }

    private void DrawDisplaysTab()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawDisplaysListPanel();
            DrawSelectedInspectorPanel();
        }
    }
}
