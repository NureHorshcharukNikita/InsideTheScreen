using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class CardCreatorWindow
{
    private void DrawCardsListPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(620)))
        {
            EditorGUILayout.LabelField("Cards", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _cardsFolder = EditorGUILayout.TextField("Cards Folder", _cardsFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newCardName = EditorGUILayout.TextField("New Card Name", _newCardName);
                    if (GUILayout.Button("Create", GUILayout.Width(110), GUILayout.Height(24)))
                        CreateCardAsset();
                }

                _cardSearch = EditorGUILayout.TextField("Search", _cardSearch);
            }

            EditorGUILayout.Space(6);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
            Rect headerRect = GUILayoutUtility.GetRect(10f, 24f, GUILayout.ExpandWidth(true));
            DrawCardsTableHeader(headerRect);
            IEnumerable<CardData> filtered = _cards.Where(MatchCardSearch);
            foreach (CardData card in filtered)
            {
                if (card == null)
                    continue;
                DrawCardRow(_cards.IndexOf(card), card);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawEffectsListPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(620)))
        {
            EditorGUILayout.LabelField("Instant Effects", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _effectsFolder = EditorGUILayout.TextField("Instant Effects Folder", _effectsFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newEffectName = EditorGUILayout.TextField("New Effect Name", _newEffectName);
                    if (_effectTypes.Count > 0)
                        _effectTypeIndex = EditorGUILayout.Popup(_effectTypeIndex, _effectTypeNames, GUILayout.Width(170));

                    using (new EditorGUI.DisabledScope(_effectTypes.Count == 0))
                    {
                        if (GUILayout.Button("Create", GUILayout.Width(110), GUILayout.Height(24)))
                            CreateEffectAsset();
                    }
                }

                _effectSearch = EditorGUILayout.TextField("Search", _effectSearch);
            }

            EditorGUILayout.Space(6);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
            EditorTableGUI.DrawHeader(new[] { "Name", "Type", "Description" }, _assetRowWidths);
            IEnumerable<InstantBattleEffect> filtered = _effects.Where(MatchEffectSearch);
            foreach (InstantBattleEffect effect in filtered)
            {
                if (effect == null)
                    continue;
                DrawEffectRow(_effects.IndexOf(effect), effect);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawSelectedInspectorPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            EditorGUILayout.LabelField("Inspector", EditorStyles.boldLabel);
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll, GUI.skin.box);
            if (_selectedAssetEditor != null)
                _selectedAssetEditor.OnInspectorGUI();
            else
                EditorGUILayout.HelpBox("Select a card or effect from the left list.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawStatusesListPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(620)))
        {
            EditorGUILayout.LabelField("Battle Statuses", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _statusesFolder = EditorGUILayout.TextField("Statuses Folder", _statusesFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newStatusName = EditorGUILayout.TextField("New Status Name", _newStatusName);
                    if (_statusTypes.Count > 0)
                        _statusTypeIndex = EditorGUILayout.Popup(_statusTypeIndex, _statusTypeNames, GUILayout.Width(170));

                    using (new EditorGUI.DisabledScope(_statusTypes.Count == 0))
                    {
                        if (GUILayout.Button("Create", GUILayout.Width(110), GUILayout.Height(24)))
                            CreateStatusAsset();
                    }
                }

                _statusSearch = EditorGUILayout.TextField("Search", _statusSearch);
            }

            EditorGUILayout.Space(6);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
            EditorTableGUI.DrawHeader(new[] { "Name", "Type", "Asset" }, _assetRowWidths);
            foreach (BattleStatus status in _statuses.Where(MatchStatusSearch))
            {
                if (status == null)
                    continue;
                DrawStatusRow(_statuses.IndexOf(status), status);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawDisplaysListPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(620)))
        {
            EditorGUILayout.LabelField("Effect Displays", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _displaysFolder = EditorGUILayout.TextField("Displays Folder", _displaysFolder);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _newDisplayName = EditorGUILayout.TextField("New Display Name", _newDisplayName);
                    if (GUILayout.Button("Create", GUILayout.Width(110), GUILayout.Height(24)))
                        CreateDisplayAsset();
                }

                _displaySearch = EditorGUILayout.TextField("Search", _displaySearch);
            }

            EditorGUILayout.Space(6);
            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll, GUI.skin.box);
            EditorTableGUI.DrawHeader(new[] { "Name", "Type", "Asset" }, _assetRowWidths);
            foreach (BattleEffectDisplayData display in _displays.Where(MatchDisplaySearch))
            {
                if (display == null)
                    continue;
                DrawDisplayRow(_displays.IndexOf(display), display);
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawCardsTableHeader(Rect rowRect)
    {
        DrawTableBackground(rowRect, new Color(0f, 0f, 0f, 0.14f));
        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 3f, _cardColName - 6f, rowRect.height - 6f);
        Rect idRect = new Rect(nameRect.xMax + 6f, rowRect.y + 3f, _cardColId - 6f, rowRect.height - 6f);
        Rect costRect = new Rect(idRect.xMax + 6f, rowRect.y + 3f, _cardColCost - 6f, rowRect.height - 6f);
        Rect assetRect = new Rect(costRect.xMax + 6f, rowRect.y + 3f, Mathf.Max(40f, rowRect.width - (costRect.xMax - rowRect.x) - 12f), rowRect.height - 6f);

        EditorGUI.LabelField(nameRect, "Name", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(idRect, "ID", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(costRect, "Cost", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(assetRect, "Asset", EditorStyles.miniBoldLabel);

        DrawVerticalLine(nameRect.xMax + 3f, rowRect);
        DrawVerticalLine(idRect.xMax + 3f, rowRect);
        DrawVerticalLine(costRect.xMax + 3f, rowRect);

        HandleColumnResize("card_name", new Rect(nameRect.xMax + 1f, rowRect.y, 6f, rowRect.height), ref _cardColName, 120f, 420f);
        HandleColumnResize("card_id", new Rect(idRect.xMax + 1f, rowRect.y, 6f, rowRect.height), ref _cardColId, 80f, 320f);
        HandleColumnResize("card_cost", new Rect(costRect.xMax + 1f, rowRect.y, 6f, rowRect.height), ref _cardColCost, 50f, 140f);
    }

    private void DrawCardRow(int index, CardData card)
    {
        string displayName = string.IsNullOrWhiteSpace(card.CardName) ? card.name : card.CardName;
        string id = string.IsNullOrWhiteSpace(card.CardID) ? "-" : card.CardID;
        string assetPath = AssetDatabase.GetAssetPath(card);
        bool selected = _selectedCardIndex == index;
        Rect rowRect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rowRect, selected ? new Color(0.24f, 0.45f, 0.85f, 0.45f) : new Color(0f, 0f, 0f, 0.08f));

        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, _cardColName - 6f, rowRect.height - 4f);
        Rect idRect = new Rect(nameRect.xMax + 6f, rowRect.y + 2f, _cardColId - 6f, rowRect.height - 4f);
        Rect costRect = new Rect(idRect.xMax + 6f, rowRect.y + 2f, _cardColCost - 6f, rowRect.height - 4f);
        Rect assetRect = new Rect(costRect.xMax + 6f, rowRect.y + 2f, Mathf.Max(40f, rowRect.width - (costRect.xMax - rowRect.x) - 12f), rowRect.height - 4f);

        EditorGUI.LabelField(nameRect, displayName);
        EditorGUI.LabelField(idRect, id);
        EditorGUI.LabelField(costRect, card.Cost.ToString());
        EditorGUI.LabelField(assetRect, assetPath);
        DrawVerticalLine(nameRect.xMax + 3f, rowRect);
        DrawVerticalLine(idRect.xMax + 3f, rowRect);
        DrawVerticalLine(costRect.xMax + 3f, rowRect);
        HandleRowSelectionAndContextMenu(rowRect, index, isCardRow: true);
    }

    private void DrawEffectRow(int index, InstantBattleEffect effect)
    {
        string displayName = string.IsNullOrWhiteSpace(effect.EffectName) ? effect.name : effect.EffectName;
        string desc = string.IsNullOrWhiteSpace(effect.Description) ? "-" : effect.Description.Replace("\n", " ");
        EditorTableGUI.DrawRow(
            new[] { displayName, effect.GetType().Name, desc },
            _assetRowWidths,
            _selectedEffectIndex == index,
            () => SelectEffect(index),
            ShowDeleteContextMenu);
    }

    private void DrawStatusRow(int index, BattleStatus status)
    {
        string displayName = string.IsNullOrWhiteSpace(status.StatusName) ? status.name : status.StatusName;
        EditorTableGUI.DrawRow(
            new[] { displayName, status.GetType().Name, AssetDatabase.GetAssetPath(status) },
            _assetRowWidths,
            _selectedStatusIndex == index,
            () => SelectStatus(index),
            ShowDeleteContextMenu);
    }

    private void DrawDisplayRow(int index, BattleEffectDisplayData display)
    {
        EditorTableGUI.DrawRow(
            new[] { display.name, display.GetType().Name, AssetDatabase.GetAssetPath(display) },
            _assetRowWidths,
            _selectedDisplayIndex == index,
            () => SelectDisplay(index),
            ShowDeleteContextMenu);
    }

    private enum RowKind
    {
        Card,
        InstantEffect,
        Status,
        Display
    }

    private void HandleRowSelectionAndContextMenu(Rect rowRect, int index, bool isCardRow)
    {
        HandleRowSelectionAndContextMenu(rowRect, index, isCardRow ? RowKind.Card : RowKind.InstantEffect);
    }

    private void HandleRowSelectionAndContextMenu(Rect rowRect, int index, RowKind rowKind)
    {
        Event currentEvent = Event.current;
        if (!rowRect.Contains(currentEvent.mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            SelectRow(index, rowKind);
            currentEvent.Use();
            return;
        }

        if (currentEvent.type == EventType.ContextClick)
        {
            SelectRow(index, rowKind);
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedAssetWithConfirm);
            menu.ShowAsContext();
            currentEvent.Use();
        }
    }

    private void SelectRow(int index, RowKind rowKind)
    {
        if (rowKind == RowKind.Card)
            SelectCard(index);
        else if (rowKind == RowKind.InstantEffect)
            SelectEffect(index);
        else if (rowKind == RowKind.Status)
            SelectStatus(index);
        else
            SelectDisplay(index);
    }

    private void ShowDeleteContextMenu()
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedAssetWithConfirm);
        menu.ShowAsContext();
    }

    private static void DrawTableBackground(Rect rect, Color color)
    {
        EditorGUI.DrawRect(rect, color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.06f));
    }

    private static void DrawVerticalLine(float x, Rect rowRect)
    {
        EditorGUI.DrawRect(new Rect(x, rowRect.y, 1f, rowRect.height), new Color(1f, 1f, 1f, 0.06f));
    }

    private void HandleColumnResize(string key, Rect handleRect, ref float width, float min, float max)
    {
        Event currentEvent = Event.current;
        EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && handleRect.Contains(currentEvent.mousePosition))
        {
            _isResizingColumn = true;
            _resizingColumnKey = key;
            _resizeMouseStart = currentEvent.mousePosition;
            _resizeWidthStart = width;
            currentEvent.Use();
        }

        if (_isResizingColumn && _resizingColumnKey == key)
        {
            if (currentEvent.type == EventType.MouseDrag)
            {
                width = Mathf.Clamp(_resizeWidthStart + (currentEvent.mousePosition.x - _resizeMouseStart.x), min, max);
                Repaint();
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp || currentEvent.rawType == EventType.MouseUp)
            {
                _isResizingColumn = false;
                _resizingColumnKey = null;
                currentEvent.Use();
            }
        }
    }
}
