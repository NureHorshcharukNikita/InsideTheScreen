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
            .SelectMany(card => card.Effects)
            .Where(effectEntry => effectEntry.effect != null)
            .Select(effectEntry => effectEntry.effect.GetType())
            .Distinct()
            .OrderBy(type => type.Name);

        foreach (Type category in categories)
            CreateButton(category.Name, category);

        UpdateVisualSelection();
        onSelected?.Invoke(null);
    }

    private void CreateButton(string label, Type category)
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
}
