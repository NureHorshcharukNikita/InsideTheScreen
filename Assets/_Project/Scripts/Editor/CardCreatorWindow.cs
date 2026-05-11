using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class CardCreatorWindow : EditorWindow
{
    private const string DefaultCardsFolder = "Assets/_Project/ScriptableObjects/Cards";
    private const string DefaultEffectsFolder = "Assets/_Project/ScriptableObjects/Effects";

    private readonly List<CardData> _cards = new();
    private readonly List<BattleEffect> _effects = new();
    private readonly List<Type> _effectTypes = new();

    private string[] _effectTypeNames = Array.Empty<string>();
    private int _effectTypeIndex;
    private int _tabIndex;
    private int _selectedCardIndex = -1;
    private int _selectedEffectIndex = -1;

    private string _cardsFolder = DefaultCardsFolder;
    private string _effectsFolder = DefaultEffectsFolder;
    private string _newCardName = "NewCard";
    private string _newEffectName = "NewEffect";
    private string _cardSearch = "";
    private string _effectSearch = "";

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
        _tabIndex = GUILayout.Toolbar(_tabIndex, new[] { "Cards", "Effects" });
        EditorGUILayout.Space(6);

        if (_tabIndex == 0)
            DrawCardsTab();
        else
            DrawEffectsTab();
    }

    private void DrawTopBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Card Tools", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            bool hasSelection = _selectedCardIndex >= 0 || _selectedEffectIndex >= 0;
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
}
