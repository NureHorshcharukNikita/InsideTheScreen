using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class InventoryCategoriesPanel : MonoBehaviour
{
    [SerializeField] private CategoryButton buttonPrefab;
    [SerializeField] private Transform container;

    private Action<Type> onSelected;
    private readonly List<(Type category, CategoryButton button)> buttons = new();

    private Type currentSelectedCategory;

    public void Build(IEnumerable<CardData> cards, Action<Type> onCategorySelected)
    {
        onSelected = onCategorySelected;
        buttons.Clear();
        currentSelectedCategory = null;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        CreateButton("All", null);

        var categories = cards
            .SelectMany(c => c.Effects)
            .Where(e => e.effect != null)
            .Select(e => e.effect.GetType())
            .Distinct()
            .OrderBy(t => t.Name);

        foreach (var category in categories)
            CreateButton(category.Name, category);

        UpdateVisualSelection();
        onSelected?.Invoke(null);
    }

    private void CreateButton(string label, Type category)
    {
        var btn = Instantiate(buttonPrefab, container);

        btn.Init(label, () =>
        {
            currentSelectedCategory = category;
            UpdateVisualSelection();
            onSelected?.Invoke(category);
        }, category == currentSelectedCategory);

        buttons.Add((category, btn));
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
}