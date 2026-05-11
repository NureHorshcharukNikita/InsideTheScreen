using UnityEngine;

public sealed partial class CardBattleDragHandler
{
    private bool TryScreenToCanvasLocal(Vector2 screenPosition, out Vector2 localPoint)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRootRect,
            screenPosition,
            GetUiEventCamera(),
            out localPoint);
    }

    private Camera GetUiEventCamera()
    {
        if (rootCanvas == null)
            return null;

        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
    }

    private bool TryPlayOnDropTarget(Vector2 screenPosition)
    {
        ICombatant target = ResolveDropTarget(screenPosition);
        return target != null && battleSystem.TryPlayCardFromHand(owner.CardIndex, target);
    }

    private static ICombatant ResolveDropTarget(Vector2 screenPosition)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return null;

        Vector3 world = camera.ScreenToWorldPoint(screenPosition);
        world.z = 0f;

        Collider2D hit = Physics2D.OverlapPoint(world);
        return hit != null ? hit.GetComponentInParent<ICombatant>() : null;
    }
}
