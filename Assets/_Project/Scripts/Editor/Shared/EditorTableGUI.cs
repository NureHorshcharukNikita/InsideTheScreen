using System;
using UnityEditor;
using UnityEngine;

public static class EditorTableGUI
{
    private const float RowHeight = 22f;
    private const float HeaderHeight = 24f;
    private const float CellPadding = 6f;
    private static readonly Color HeaderColor = new(0f, 0f, 0f, 0.14f);
    private static readonly Color RowColor = new(0f, 0f, 0f, 0.08f);
    private static readonly Color SelectedRowColor = new(0.24f, 0.45f, 0.85f, 0.45f);
    private static readonly Color LineColor = new(1f, 1f, 1f, 0.06f);

    public static void DrawHeader(string[] labels, float[] widths)
    {
        Rect rowRect = GUILayoutUtility.GetRect(10f, HeaderHeight, GUILayout.ExpandWidth(true));
        DrawBackground(rowRect, HeaderColor);
        DrawCells(rowRect, labels, widths, EditorStyles.miniBoldLabel);
    }

    public static void DrawRow(string[] values, float[] widths, bool selected, Action onSelect, Action onContextMenu)
    {
        Rect rowRect = GUILayoutUtility.GetRect(10f, RowHeight, GUILayout.ExpandWidth(true));
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rowRect, selected ? SelectedRowColor : RowColor);

        DrawCells(rowRect, values, widths, EditorStyles.label);
        HandleRowEvents(rowRect, onSelect, onContextMenu);
    }

    private static void DrawCells(Rect rowRect, string[] values, float[] widths, GUIStyle style)
    {
        float x = rowRect.x + CellPadding;
        float fixedWidth = 0f;
        for (int i = 0; i < widths.Length; i++)
        {
            if (widths[i] > 0f)
                fixedWidth += widths[i] + CellPadding;
        }

        for (int i = 0; i < values.Length; i++)
        {
            float width = i < widths.Length && widths[i] > 0f
                ? widths[i]
                : Mathf.Max(40f, rowRect.width - fixedWidth - CellPadding);

            Rect cellRect = new(x, rowRect.y + 2f, Mathf.Max(1f, width - CellPadding), rowRect.height - 4f);
            EditorGUI.LabelField(cellRect, values[i], style);
            x += width + CellPadding;

            if (i < values.Length - 1)
                DrawVerticalLine(x - 3f, rowRect);
        }
    }

    private static void HandleRowEvents(Rect rowRect, Action onSelect, Action onContextMenu)
    {
        Event currentEvent = Event.current;
        if (!rowRect.Contains(currentEvent.mousePosition))
            return;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            onSelect?.Invoke();
            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.ContextClick)
        {
            onSelect?.Invoke();
            onContextMenu?.Invoke();
            currentEvent.Use();
        }
    }

    private static void DrawBackground(Rect rect, Color color)
    {
        EditorGUI.DrawRect(rect, color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), LineColor);
    }

    private static void DrawVerticalLine(float x, Rect rowRect)
    {
        EditorGUI.DrawRect(new Rect(x, rowRect.y, 1f, rowRect.height), LineColor);
    }
}
