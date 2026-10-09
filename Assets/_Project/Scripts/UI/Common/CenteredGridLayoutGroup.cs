using UnityEngine;
using UnityEngine.UI;

// Grid that splits the available width into equal columns and centers an incomplete last row.
// A single child takes the whole width and the single cell height.
public sealed class CenteredGridLayoutGroup : LayoutGroup
{
    [SerializeField, Min(1)] private int columns = 2;
    [SerializeField, Min(1f)] private float cellHeight = 190f;
    [SerializeField, Min(1f)] private float singleCellHeight = 220f;
    [SerializeField] private Vector2 spacing = new(8f, 8f);

    private int ColumnCount => rectChildren.Count <= 1 ? 1 : columns;
    private float CellHeight => rectChildren.Count <= 1 ? singleCellHeight : cellHeight;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        SetLayoutInputForAxis(padding.horizontal, padding.horizontal, -1f, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        float height = padding.vertical;
        if (rectChildren.Count > 0)
        {
            int rows = Mathf.CeilToInt(rectChildren.Count / (float)ColumnCount);
            height += rows * CellHeight + (rows - 1) * spacing.y;
        }

        SetLayoutInputForAxis(height, height, -1f, 1);
    }

    public override void SetLayoutHorizontal()
    {
        int columnCount = ColumnCount;
        float innerWidth = rectTransform.rect.width - padding.horizontal;
        float cellWidth = Mathf.Max(0f, (innerWidth - spacing.x * (columnCount - 1)) / columnCount);

        for (int i = 0; i < rectChildren.Count; i++)
        {
            int row = i / columnCount;
            int column = i % columnCount;
            int itemsInRow = Mathf.Min(columnCount, rectChildren.Count - row * columnCount);
            float rowWidth = itemsInRow * cellWidth + (itemsInRow - 1) * spacing.x;
            float rowStart = padding.left + (innerWidth - rowWidth) * 0.5f;

            SetChildAlongAxis(rectChildren[i], 0, rowStart + column * (cellWidth + spacing.x), cellWidth);
        }
    }

    public override void SetLayoutVertical()
    {
        int columnCount = ColumnCount;
        float height = CellHeight;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            int row = i / columnCount;
            SetChildAlongAxis(rectChildren[i], 1, padding.top + row * (height + spacing.y), height);
        }
    }
}
