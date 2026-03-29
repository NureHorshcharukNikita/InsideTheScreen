using UnityEngine;

public static class InventoryViewUtils
{
    public static void CleanupCardViews(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);

            if (child.TryGetComponent<CardView>(out _))
            {
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    public static void Cleanup(params Transform[] parents)
    {
        foreach (var parent in parents)
            CleanupCardViews(parent);
    }
}