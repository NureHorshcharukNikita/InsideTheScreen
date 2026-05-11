using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class CardCreatorWindow
{
    private void CreateCardAsset()
    {
        EnsureFolderExists(_cardsFolder);
        string safeName = string.IsNullOrWhiteSpace(_newCardName) ? "NewCard" : _newCardName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_cardsFolder}/{safeName}.asset");
        CardData card = CreateInstance<CardData>();
        AssetDatabase.CreateAsset(card, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshAssets();

        CardData createdCard = AssetDatabase.LoadAssetAtPath<CardData>(path);
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
        EnsureFolderExists(_effectsFolder);
        string safeName = string.IsNullOrWhiteSpace(_newEffectName) ? "NewEffect" : _newEffectName.Trim();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{_effectsFolder}/{safeName}.asset");
        Type effectType = _effectTypes[Mathf.Clamp(_effectTypeIndex, 0, _effectTypes.Count - 1)];
        BattleEffect effect = (BattleEffect)CreateInstance(effectType);
        AssetDatabase.CreateAsset(effect, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RefreshAssets();

        BattleEffect createdEffect = AssetDatabase.LoadAssetAtPath<BattleEffect>(path);
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

    private void SelectCard(int index)
    {
        if (index < 0 || index >= _cards.Count)
            return;

        _selectedCardIndex = index;
        _selectedEffectIndex = -1;
        SetSelectedAsset(_cards[index]);
    }

    private void SelectEffect(int index)
    {
        if (index < 0 || index >= _effects.Count)
            return;

        _selectedEffectIndex = index;
        _selectedCardIndex = -1;
        SetSelectedAsset(_effects[index]);
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

        foreach (string guid in AssetDatabase.FindAssets("t:CardData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card != null)
                _cards.Add(card);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BattleEffect effect = AssetDatabase.LoadAssetAtPath<BattleEffect>(path);
            if (effect != null)
                _effects.Add(effect);
        }

        _cards.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _effects.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
        _selectedCardIndex = -1;
        _selectedEffectIndex = -1;
        SetSelectedAsset(null);
    }

    private bool MatchCardSearch(CardData card)
    {
        if (string.IsNullOrWhiteSpace(_cardSearch))
            return true;

        string query = _cardSearch.Trim();
        return ContainsIgnoreCase(card.name, query)
            || ContainsIgnoreCase(card.CardName, query)
            || ContainsIgnoreCase(card.CardID, query)
            || ContainsIgnoreCase(card.Description, query);
    }

    private bool MatchEffectSearch(BattleEffect effect)
    {
        if (string.IsNullOrWhiteSpace(_effectSearch))
            return true;

        string query = _effectSearch.Trim();
        return ContainsIgnoreCase(effect.name, query)
            || ContainsIgnoreCase(effect.EffectName, query)
            || ContainsIgnoreCase(effect.Description, query)
            || ContainsIgnoreCase(effect.GetType().Name, query);
    }

    private static bool ContainsIgnoreCase(string source, string query)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(query))
            return false;

        return source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
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
