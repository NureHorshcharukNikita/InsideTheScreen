using UnityEngine;

public partial class HealthBarUI
{
    public void SnapToSpriteWorldAnchor()
    {
        if (target == null || _canvas == null)
            return;

        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        Bounds local = spriteRenderer.localBounds;
        float yLocal = local.min.y;
        Vector3 anchorLocal = new Vector3(local.center.x, yLocal, local.center.z);
        Vector3 worldPoint = spriteRenderer.transform.TransformPoint(anchorLocal);

        Vector3 screenPoint = camera.WorldToScreenPoint(worldPoint);
        if (screenPoint.z <= 0f)
            return;

        Camera uiEventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camera;

        RectTransform canvasRect = (RectTransform)_canvas.transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                uiEventCamera,
                out Vector2 localPoint))
            return;

        float fromPivotToTop = (1f - _rect.pivot.y) * _rect.rect.height;
        localPoint.y -= fromPivotToTop + belowSpritePadding;

        _rect.anchoredPosition = localPoint;
    }

}
