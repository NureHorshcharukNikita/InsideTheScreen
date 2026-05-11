public partial class InventoryWindow
{
    public void SelectTopIcon(int index)
    {
        if (index < 0 || index >= topIconBorders.Count)
            return;

        selectedTopIconIndex = index;

        categoriesPanel.ResetToAll();

        inventoryScreenController.ClearSelection();
        Refresh();

        for (int i = 0; i < topIconBorders.Count; i++)
        {
            if (topIconBorders[i] != null)
                topIconBorders[i].SetActive(i == index);
        }

        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == index);
        }
    }

    public void SelectNextTopIcon()
    {
        if (topIconBorders.Count == 0)
            return;

        int next = selectedTopIconIndex + 1;
        if (next >= topIconBorders.Count)
            next = 0;

        SelectTopIcon(next);
    }

    public void SelectPreviousTopIcon()
    {
        if (topIconBorders.Count == 0)
            return;

        int prev = selectedTopIconIndex - 1;
        if (prev < 0)
            prev = topIconBorders.Count - 1;

        SelectTopIcon(prev);
    }
}
