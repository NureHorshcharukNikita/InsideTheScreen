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
            EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _effectsFolder = EditorGUILayout.TextField("Effects Folder", _effectsFolder);
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
            Rect headerRect = GUILayoutUtility.GetRect(10f, 24f, GUILayout.ExpandWidth(true));
            DrawEffectsTableHeader(headerRect);
            IEnumerable<BattleEffect> filtered = _effects.Where(MatchEffectSearch);
            foreach (BattleEffect effect in filtered)
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

    private void DrawCardsTableHeader(Rect rowRect)
    {
        DrawTableBackground(rowRect, new Color(0f, 0f, 0f, 0.14f));
        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 3f, _cardColName - 6f, rowRect.height - 6f);
        Rect idRect = new Rect(nameRect.xMax + 6f, rowRect.y + 3f, _cardColId - 6f, rowRect.height - 6f);
        Rect costRect = new Rect(idRect.xMax + 6f, rowRect.y + 3f, _cardColCost - 6f, rowRect.height - 6f);
        Rect effectsRect = new Rect(costRect.xMax + 6f, rowRect.y + 3f, _cardColEffects - 6f, rowRect.height - 6f);
        Rect assetRect = new Rect(effectsRect.xMax + 6f, rowRect.y + 3f, Mathf.Max(40f, rowRect.width - (effectsRect.xMax - rowRect.x) - 12f), rowRect.height - 6f);

        EditorGUI.LabelField(nameRect, "Name", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(idRect, "ID", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(costRect, "Cost", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(effectsRect, "Effects", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(assetRect, "Asset", EditorStyles.miniBoldLabel);

        DrawVerticalLine(nameRect.xMax + 3f, rowRect);
        DrawVerticalLine(idRect.xMax + 3f, rowRect);
        DrawVerticalLine(costRect.xMax + 3f, rowRect);
        DrawVerticalLine(effectsRect.xMax + 3f, rowRect);

        HandleColumnResize("card_name", new Rect(nameRect.xMax + 1f, rowRect.y, 6f, rowRect.height), ref _cardColName, 120f, 420f);
        HandleColumnResize("card_id", new Rect(idRect.xMax + 1f, rowRect.y, 6f, rowRect.height), ref _cardColId, 80f, 320f);
        HandleColumnResize("card_cost", new Rect(costRect.xMax + 1f, rowRect.y, 6f, rowRect.height), ref _cardColCost, 50f, 140f);
        HandleColumnResize("card_effects", new Rect(effectsRect.xMax + 1f, rowRect.y, 6f, rowRect.height), ref _cardColEffects, 60f, 160f);
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
        Rect effectsRect = new Rect(costRect.xMax + 6f, rowRect.y + 2f, _cardColEffects - 6f, rowRect.height - 4f);
        Rect assetRect = new Rect(effectsRect.xMax + 6f, rowRect.y + 2f, Mathf.Max(40f, rowRect.width - (effectsRect.xMax - rowRect.x) - 12f), rowRect.height - 4f);

        EditorGUI.LabelField(nameRect, displayName);
        EditorGUI.LabelField(idRect, id);
        EditorGUI.LabelField(costRect, card.Cost.ToString());
        EditorGUI.LabelField(effectsRect, card.Effects.Count.ToString());
        EditorGUI.LabelField(assetRect, assetPath);
        DrawVerticalLine(nameRect.xMax + 3f, rowRect);
        DrawVerticalLine(idRect.xMax + 3f, rowRect);
        DrawVerticalLine(costRect.xMax + 3f, rowRect);
        DrawVerticalLine(effectsRect.xMax + 3f, rowRect);
        HandleRowSelectionAndContextMenu(rowRect, index, isCardRow: true);
    }

    private void DrawEffectsTableHeader(Rect rowRect)
    {
        DrawTableBackground(rowRect, new Color(0f, 0f, 0f, 0.14f));
        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 3f, _effectColName - 6f, rowRect.height - 6f);
        Rect typeRect = new Rect(nameRect.xMax + 6f, rowRect.y + 3f, _effectColType - 6f, rowRect.height - 6f);
        Rect descriptionRect = new Rect(typeRect.xMax + 6f, rowRect.y + 3f, Mathf.Max(40f, rowRect.width - (typeRect.xMax - rowRect.x) - 12f), rowRect.height - 6f);
        EditorGUI.LabelField(nameRect, "Name", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(typeRect, "Type", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(descriptionRect, "Description", EditorStyles.miniBoldLabel);
        DrawVerticalLine(nameRect.xMax + 3f, rowRect);
        DrawVerticalLine(typeRect.xMax + 3f, rowRect);
        HandleColumnResize("effect_name", new Rect(nameRect.xMax + 1f, rowRect.y, 6f, rowRect.height), ref _effectColName, 120f, 420f);
        HandleColumnResize("effect_type", new Rect(typeRect.xMax + 1f, rowRect.y, 6f, rowRect.height), ref _effectColType, 100f, 320f);
    }

    private void DrawEffectRow(int index, BattleEffect effect)
    {
        string displayName = string.IsNullOrWhiteSpace(effect.EffectName) ? effect.name : effect.EffectName;
        string typeName = effect.GetType().Name;
        string desc = string.IsNullOrWhiteSpace(effect.Description) ? "-" : effect.Description.Replace("\n", " ");
        bool selected = _selectedEffectIndex == index;
        Rect rowRect = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rowRect, selected ? new Color(0.24f, 0.45f, 0.85f, 0.45f) : new Color(0f, 0f, 0f, 0.08f));

        Rect nameRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, _effectColName - 6f, rowRect.height - 4f);
        Rect typeRect = new Rect(nameRect.xMax + 6f, rowRect.y + 2f, _effectColType - 6f, rowRect.height - 4f);
        Rect descriptionRect = new Rect(typeRect.xMax + 6f, rowRect.y + 2f, Mathf.Max(40f, rowRect.width - (typeRect.xMax - rowRect.x) - 12f), rowRect.height - 4f);

        EditorGUI.LabelField(nameRect, displayName);
        EditorGUI.LabelField(typeRect, typeName);
        EditorGUI.LabelField(descriptionRect, desc);
        DrawVerticalLine(nameRect.xMax + 3f, rowRect);
        DrawVerticalLine(typeRect.xMax + 3f, rowRect);
        HandleRowSelectionAndContextMenu(rowRect, index, isCardRow: false);
    }

    private void HandleRowSelectionAndContextMenu(Rect rowRect, int index, bool isCardRow)
    {
        Event currentEvent = Event.current;
        if (!rowRect.Contains(currentEvent.mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            if (isCardRow) SelectCard(index); else SelectEffect(index);
            currentEvent.Use();
            return;
        }

        if (currentEvent.type == EventType.ContextClick)
        {
            if (isCardRow) SelectCard(index); else SelectEffect(index);
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Delete"), false, DeleteSelectedAssetWithConfirm);
            menu.ShowAsContext();
            currentEvent.Use();
        }
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
