using UnityEngine;

public partial class EnemyIntentView
{
    private void LateUpdate()
    {
        ResolveFollowRect();
        if (targetCharacter == null || followRectTransform == null || canvasCache == null)
            return;

        SpriteRenderer spriteRenderer = targetCharacter.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        Bounds bounds = spriteRenderer.bounds;
        Vector3 worldPoint = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        Vector3 screenPoint = camera.WorldToScreenPoint(worldPoint);
        if (screenPoint.z <= 0f)
            return;

        Camera uiEventCamera = canvasCache.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera;
        RectTransform canvasRect = canvasCache.transform as RectTransform;
        if (canvasRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiEventCamera, out Vector2 localPoint))
            return;

        float fromPivotToBottom = followRectTransform.pivot.y * followRectTransform.rect.height;
        localPoint.y += fromPivotToBottom + aboveSpritePadding;
        followRectTransform.anchoredPosition = localPoint;
    }

    private void ResolveFollowRect()
    {
        if (intentContainer != null)
        {
            RectTransform containerRect = intentContainer.transform as RectTransform;
            if (containerRect != null)
            {
                followRectTransform = containerRect;
                return;
            }
        }

        followRectTransform = rectTransformCache;
    }
}
