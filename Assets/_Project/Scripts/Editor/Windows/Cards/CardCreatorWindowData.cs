using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class CardCreatorWindow
{
    private void CreateCardAsset()
    {
        CardData createdCard = EditorAssetUtility.CreateAsset<CardData>(_cardsFolder, GetAssetNameOrFallback(_newCardName, "NewCard"));
        RefreshAssets();

        if (createdCard != null)
        {
            _tabIndex = 0;
            int index = _cards.IndexOf(createdCard);
            if (index >= 0)
                SelectCard(index);
            _rightScroll = Vector2.zero;
            Repaint();
        }
    }

    private void CreateEffectAsset()
    {
        Type effectType = _effectTypes[Mathf.Clamp(_effectTypeIndex, 0, _effectTypes.Count - 1)];
        InstantBattleEffect createdEffect = EditorAssetUtility.CreateAsset(
            effectType,
            _effectsFolder,
            GetAssetNameOrFallback(_newEffectName, "NewEffect")) as InstantBattleEffect;
        RefreshAssets();

        if (createdEffect != null)
        {
            _tabIndex = 1;
            int index = _effects.IndexOf(createdEffect);
            if (index >= 0)
                SelectEffect(index);
            _rightScroll = Vector2.zero;
            Repaint();
        }
    }

    private void CreateStatusAsset()
    {
        Type statusType = _statusTypes[Mathf.Clamp(_statusTypeIndex, 0, _statusTypes.Count - 1)];
        BattleStatus createdStatus = EditorAssetUtility.CreateAsset(
            statusType,
            _statusesFolder,
            GetAssetNameOrFallback(_newStatusName, "NewStatus")) as BattleStatus;
        RefreshAssets();

        if (createdStatus != null)
        {
            _tabIndex = 2;
            int index = _statuses.IndexOf(createdStatus);
            if (index >= 0)
                SelectStatus(index);
            _rightScroll = Vector2.zero;
            Repaint();
        }
    }

    private void CreateDisplayAsset()
    {
        BattleEffectDisplayData createdDisplay = EditorAssetUtility.CreateAsset<BattleEffectDisplayData>(
            _displaysFolder,
            GetAssetNameOrFallback(_newDisplayName, "NewDisplay"));
        RefreshAssets();

        if (createdDisplay != null)
        {
            _tabIndex = 3;
            int index = _displays.IndexOf(createdDisplay);
            if (index >= 0)
                SelectDisplay(index);
            _rightScroll = Vector2.zero;
            Repaint();
        }
    }

    private void SelectCard(int index)
    {
        if (index < 0 || index >= _cards.Count)
            return;

        _selectedCardIndex = index;
        _selectedEffectIndex = -1;
        _selectedStatusIndex = -1;
        _selectedDisplayIndex = -1;
        SetSelectedAsset(_cards[index]);
    }

    private void SelectEffect(int index)
    {
        if (index < 0 || index >= _effects.Count)
            return;

        _selectedEffectIndex = index;
        _selectedCardIndex = -1;
        _selectedStatusIndex = -1;
        _selectedDisplayIndex = -1;
        SetSelectedAsset(_effects[index]);
    }

    private void SelectStatus(int index)
    {
        if (index < 0 || index >= _statuses.Count)
            return;

        _selectedStatusIndex = index;
        _selectedCardIndex = -1;
        _selectedEffectIndex = -1;
        _selectedDisplayIndex = -1;
        SetSelectedAsset(_statuses[index]);
    }

    private void SelectDisplay(int index)
    {
        if (index < 0 || index >= _displays.Count)
            return;

        _selectedDisplayIndex = index;
        _selectedCardIndex = -1;
        _selectedEffectIndex = -1;
        _selectedStatusIndex = -1;
        SetSelectedAsset(_displays[index]);
    }

    private void SetSelectedAsset(UnityEngine.Object selected)
    {
        if (_selectedAssetEditor != null)
            DestroyImmediate(_selectedAssetEditor);

        _selectedAssetEditor = selected != null ? Editor.CreateEditor(selected) : null;
        Selection.activeObject = selected;
        if (selected != null)
            EditorGUIUtility.PingObject(selected);
    }

    private void RefreshAssets()
    {
        _cards.Clear();
        _effects.Clear();
        _statuses.Clear();
        _displays.Clear();

        _cards.AddRange(EditorAssetUtility.LoadAssets<CardData>());

        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            InstantBattleEffect effect = AssetDatabase.LoadAssetAtPath<InstantBattleEffect>(path);
            if (effect != null)
                _effects.Add(effect);

            BattleStatus status = AssetDatabase.LoadAssetAtPath<BattleStatus>(path);
            if (status != null)
                _statuses.Add(status);

            BattleEffectDisplayData display = AssetDatabase.LoadAssetAtPath<BattleEffectDisplayData>(path);
            if (display != null)
                _displays.Add(display);
        }

        _effects.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _statuses.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _displays.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _selectedCardIndex = -1;
        _selectedEffectIndex = -1;
        _selectedStatusIndex = -1;
        _selectedDisplayIndex = -1;
        SetSelectedAsset(null);
    }

    private bool MatchCardSearch(CardData card)
    {
        if (string.IsNullOrWhiteSpace(_cardSearch))
            return true;

        string query = _cardSearch.Trim();
        return EditorAssetUtility.ContainsIgnoreCase(card.name, query)
            || EditorAssetUtility.ContainsIgnoreCase(card.CardName, query)
            || EditorAssetUtility.ContainsIgnoreCase(card.CardID, query)
            || EditorAssetUtility.ContainsIgnoreCase(card.Description, query);
    }

    private bool MatchEffectSearch(InstantBattleEffect effect)
    {
        if (string.IsNullOrWhiteSpace(_effectSearch))
            return true;

        string query = _effectSearch.Trim();
        return EditorAssetUtility.ContainsIgnoreCase(effect.name, query)
            || EditorAssetUtility.ContainsIgnoreCase(effect.EffectName, query)
            || EditorAssetUtility.ContainsIgnoreCase(effect.Description, query)
            || EditorAssetUtility.ContainsIgnoreCase(effect.GetType().Name, query);
    }

    private bool MatchStatusSearch(BattleStatus status)
    {
        if (string.IsNullOrWhiteSpace(_statusSearch))
            return true;

        string query = _statusSearch.Trim();
        return EditorAssetUtility.ContainsIgnoreCase(status.name, query)
            || EditorAssetUtility.ContainsIgnoreCase(status.StatusID, query)
            || EditorAssetUtility.ContainsIgnoreCase(status.StatusName, query)
            || EditorAssetUtility.ContainsIgnoreCase(status.GetType().Name, query);
    }

    private bool MatchDisplaySearch(BattleEffectDisplayData display)
    {
        if (string.IsNullOrWhiteSpace(_displaySearch))
            return true;

        string query = _displaySearch.Trim();
        return EditorAssetUtility.ContainsIgnoreCase(display.name, query)
            || EditorAssetUtility.ContainsIgnoreCase(display.GetType().Name, query);
    }

    private void BuildEffectTypeList()
    {
        _effectTypes.Clear();
        _effectTypes.AddRange(EditorAssetUtility.LoadConcreteTypes<InstantBattleEffect>());
        _effectTypeNames = _effectTypes.Select(type => type.Name).ToArray();
        _effectTypeIndex = Mathf.Clamp(_effectTypeIndex, 0, Mathf.Max(0, _effectTypes.Count - 1));
    }

    private void BuildStatusTypeList()
    {
        _statusTypes.Clear();
        _statusTypes.AddRange(EditorAssetUtility.LoadConcreteTypes<BattleStatus>());
        _statusTypeNames = _statusTypes.Select(type => type.Name).ToArray();
        _statusTypeIndex = Mathf.Clamp(_statusTypeIndex, 0, Mathf.Max(0, _statusTypes.Count - 1));
    }

    private void DeleteSelectedAssetWithConfirm()
    {
        UnityEngine.Object selected = null;
        string title = "Delete Asset";
        string message = "Delete selected asset?";

        if (_selectedCardIndex >= 0 && _selectedCardIndex < _cards.Count)
        {
            selected = _cards[_selectedCardIndex];
            title = "Delete Card";
            message = $"Delete card '{selected.name}'?";
        }
        else if (_selectedEffectIndex >= 0 && _selectedEffectIndex < _effects.Count)
        {
            selected = _effects[_selectedEffectIndex];
            title = "Delete Effect";
            message = $"Delete effect '{selected.name}'?";
        }
        else if (_selectedStatusIndex >= 0 && _selectedStatusIndex < _statuses.Count)
        {
            selected = _statuses[_selectedStatusIndex];
            title = "Delete Battle Status";
            message = $"Delete status '{selected.name}'?";
        }
        else if (_selectedDisplayIndex >= 0 && _selectedDisplayIndex < _displays.Count)
        {
            selected = _displays[_selectedDisplayIndex];
            title = "Delete Effect Display";
            message = $"Delete display '{selected.name}'?";
        }

        if (selected == null)
            return;
        if (!EditorUtility.DisplayDialog(title, message, "Delete", "Cancel"))
            return;

        string path = AssetDatabase.GetAssetPath(selected);
        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        RefreshAssets();
    }

    private static string GetAssetNameOrFallback(string assetName, string fallback)
    {
        return string.IsNullOrWhiteSpace(assetName) ? fallback : assetName.Trim();
    }
}
