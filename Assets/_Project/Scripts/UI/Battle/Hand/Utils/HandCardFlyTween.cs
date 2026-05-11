using UnityEngine;

public static class HandCardFlyTween
{
    public static void SetCardFlyVisible(Transform t, bool visible)
    {
        CanvasGroup cg = t.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = t.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = visible;
        cg.interactable = visible;
    }
}
