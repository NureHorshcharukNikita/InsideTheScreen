using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCategoriesPanel : MonoBehaviour
{
    [Serializable]
    private sealed class CategoryIconEntry
    {
        public CardCategory category = default;
        public Sprite icon = null;
    }

    [SerializeField] private CategoryButton buttonPrefab;
    [SerializeField] private Transform container;

    [Header("Category Icons")]
    [SerializeField] private Sprite allCategoryIcon;
    [SerializeField] private List<CategoryIconEntry> categoryIcons = new();

    private Action<CardCategory?> onSelected;
    private readonly List<(CardCategory? category, CategoryButton button)> buttons = new();

    private CardCategory? currentSelectedCategory;
    private ScrollRect scrollRect;
    private GridLayoutGroup gridLayout;
    private float lastViewportWidth = -1f;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        gridLayout = container != null ? container.GetComponent<GridLayoutGroup>() : null;
    }

    private void LateUpdate()
    {
        FitButtonsToViewport();
    }

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

    private void FitButtonsToViewport()
    {
        if (scrollRect == null || scrollRect.viewport == null || gridLayout == null)
            return;

        float viewportWidth = scrollRect.viewport.rect.width;
        if (viewportWidth <= 0f || Mathf.Approximately(viewportWidth, lastViewportWidth))
            return;

        lastViewportWidth = viewportWidth;
        float buttonWidth = Mathf.Max(1f, viewportWidth - gridLayout.padding.horizontal);
        gridLayout.cellSize = new Vector2(buttonWidth, gridLayout.cellSize.y);
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

        button.Init(label, GetCategoryIcon(category), () =>
        {
            currentSelectedCategory = category;
            UpdateVisualSelection();
            onSelected?.Invoke(category);
        }, category == currentSelectedCategory);

        buttons.Add((category, button));
    }

    private Sprite GetCategoryIcon(CardCategory? category)
    {
        if (!category.HasValue)
            return allCategoryIcon;

        foreach (CategoryIconEntry entry in categoryIcons)
        {
            if (entry != null && entry.category == category.Value)
                return entry.icon;
        }

        return null;
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
