using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryCategoriesPanel : MonoBehaviour
{
    [SerializeField] private CategoryButton buttonPrefab;
    [SerializeField] private Transform container;

    private Action<string> onSelected;
    private readonly List<(string category, CategoryButton button)> buttons = new();

    private string currentSelectedCategory;

    public void Build(IEnumerable<CardData> cards, Action<string> onCategorySelected)
    {
        onSelected = onCategorySelected;
        buttons.Clear();
        currentSelectedCategory = null;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        CreateButton("All", null);

        var categories = cards
            .SelectMany(card => card.Categories)
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Select(category => category.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category);

        foreach (string category in categories)
            CreateButton(category, category);

        UpdateVisualSelection();
        onSelected?.Invoke(null);
    }

    private void CreateButton(string label, string category)
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
            bool isSelected = string.Equals(item.category, currentSelectedCategory, StringComparison.OrdinalIgnoreCase);
            item.button.SetSelected(isSelected);
        }
    }
}
