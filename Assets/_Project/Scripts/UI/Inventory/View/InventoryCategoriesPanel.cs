using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryCategoriesPanel : MonoBehaviour
{
    [SerializeField] private CategoryButton buttonPrefab;
    [SerializeField] private Transform container;

    private Action<CardCategory?> onSelected;
    private readonly List<(CardCategory? category, CategoryButton button)> buttons = new();

    private CardCategory? currentSelectedCategory;

    public void Build(IEnumerable<CardData> cards, Action<CardCategory?> onCategorySelected)
    {
        ClearButtons();
        onSelected = onCategorySelected;

        CreateButton("All", null);

        var categories = cards
            .SelectMany(card => card.Categories)
            .Distinct()
            .OrderBy(category => category);

        foreach (CardCategory category in categories)
            CreateButton(GetCategoryLabel(category), category);

        UpdateVisualSelection();
        onSelected?.Invoke(null);
    }

    public void ClearButtons()
    {
        onSelected = null;
        buttons.Clear();
        currentSelectedCategory = null;

        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    private void CreateButton(string label, CardCategory? category)
    {
        CategoryButton button = Instantiate(buttonPrefab, container);

        button.Init(label, () =>
        {
            currentSelectedCategory = category;
            UpdateVisualSelection();
            onSelected?.Invoke(category);
        }, category == currentSelectedCategory);

        buttons.Add((category, button));
    }

    public void ResetToAll()
    {
        currentSelectedCategory = null;
        UpdateVisualSelection();
        onSelected?.Invoke(null);
    }

    private void UpdateVisualSelection()
    {
        foreach (var item in buttons)
        {
            bool isSelected = item.category == currentSelectedCategory;
            item.button.SetSelected(isSelected);
        }
    }

    private static string GetCategoryLabel(CardCategory category)
    {
        string text = category.ToString();
        for (int i = text.Length - 1; i > 0; i--)
        {
            if (char.IsUpper(text[i]) && !char.IsWhiteSpace(text[i - 1]))
                text = text.Insert(i, " ");
        }

        return text;
    }
}
