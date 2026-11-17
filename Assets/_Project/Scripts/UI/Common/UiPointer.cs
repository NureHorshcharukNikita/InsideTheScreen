using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Which side of what it shows the pointer stands on. Auto: above what is in the lower half of the
// screen, below what is in the upper half.
public enum PointerSide
{
    Auto,
    Above,
    Below,
    Left,
    Right
}

// The arrow that shows the player where to look, e.g. at the Add to deck button while Dad has Haru
// pick his cards, or at the strength counter while he explains it in the first fight. It stands
// beside what it shows, bobbing towards it, and what it shows pulses (UiPointerTarget.Pulse). One per
// canvas, in the hierarchy, its sprite pointing right; it is clear while it shows nothing. On unscaled
// time: story events pause the game.
[RequireComponent(typeof(CanvasGroup))]
public sealed class UiPointer : MonoBehaviour
{
    private static readonly List<UiPointer> All = new();

    [Tooltip("How far from the edge of what it shows, in canvas units.")]
    [SerializeField, Min(0f)] private float gap = 10f;
    [SerializeField, Min(0f)] private float bobDistance = 12f;
    [SerializeField, Min(0f)] private float bobSpeed = 6f;
    [Tooltip("How much bigger what it shows grows at the top of a pulse.")]
    [SerializeField, Min(0f)] private float pulseScale = 0.06f;
    [SerializeField, Min(0f)] private float pulseSpeed = 5f;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.15f;

    [Header("Caption")]
    [Tooltip("Optional: a box just past the arrow's tail that says what to do, e.g. \"Pick a card to take into the fight\" in Dad's deck lesson. Its own object beside the arrow, not under it, since the arrow turns. Empty = the arrow alone.")]
    [SerializeField] private CanvasGroup caption;
    [SerializeField] private TMP_Text captionText;
    [Tooltip("How far past the arrow's tail the caption stands, in canvas units.")]
    [SerializeField, Min(0f)] private float captionGap = 6f;

    private RectTransform rect;
    private CanvasGroup group;
    private Canvas rootCanvas;
    private RectTransform target;
    private PointerSide side;
    private Transform pulsing;
    private Vector3 pulsingScale;
    private float shownSince;
    private string captionLine;

    private void Awake()
    {
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;
        if (caption != null)
        {
            caption.interactable = false;
            caption.blocksRaycasts = false;
            caption.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        Hide();
    }

    // Points at the target with this id (UiPointerTarget) on whichever canvas it is. The caption, if
    // any, is said past the arrow on a pointer that has a caption box.
    public static bool TryPointAt(string id, PointerSide side = PointerSide.Auto, string captionLine = null)
    {
        if (!UiPointerTarget.TryFind(id, out UiPointerTarget found))
            return false;

        return TryPointAt((RectTransform)found.transform, side, found.Pulse, captionLine);
    }

    public static bool TryPointAt(RectTransform target, PointerSide side = PointerSide.Auto, bool pulse = false, string captionLine = null)
    {
        if (target == null)
            return false;

        Canvas canvas = target.GetComponentInParent<Canvas>();
        Canvas root = canvas != null ? canvas.rootCanvas : null;
        foreach (UiPointer pointer in All)
        {
            if (pointer != null && pointer.RootCanvas == root)
            {
                pointer.Show(target, side, pulse, captionLine);
                return true;
            }
        }

        return false;
    }

    // The far end of the arrow that shows something on this canvas, at the farthest of its bob, in
    // world space, and the way it points: a line about what it shows is said just past it
    // (BattleTalkBubble).
    public static bool TryGetTail(Canvas canvas, out Vector3 tail, out Vector2 towardsTarget)
    {
        Canvas root = canvas != null ? canvas.rootCanvas : null;
        foreach (UiPointer pointer in All)
        {
            if (pointer == null || pointer.RootCanvas != root || !pointer.TryGetAim(out Vector2 edge, out towardsTarget))
                continue;

            float length = pointer.rect.rect.width;
            tail = pointer.rect.parent.TransformPoint(edge - towardsTarget * (pointer.gap + length + pointer.bobDistance));
            return true;
        }

        tail = default;
        towardsTarget = default;
        return false;
    }

    public static void HideAll()
    {
        foreach (UiPointer pointer in All)
        {
            if (pointer != null)
                pointer.Hide();
        }
    }

    private Canvas RootCanvas
    {
        get
        {
            if (rootCanvas == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                rootCanvas = canvas != null ? canvas.rootCanvas : null;
            }

            return rootCanvas;
        }
    }

    private void Show(RectTransform newTarget, PointerSide newSide, bool pulse, string newCaption)
    {
        newCaption = string.IsNullOrWhiteSpace(newCaption) ? null : newCaption;
        if (newTarget == target && newSide == side && newCaption == captionLine)
            return;

        StopPulse();
        if (target == null)
            shownSince = Time.unscaledTime;

        target = newTarget;
        side = newSide;
        captionLine = newCaption;
        if (pulse)
        {
            pulsing = target;
            pulsingScale = target.localScale;
        }

        if (caption != null && captionText != null)
        {
            captionText.text = captionLine ?? string.Empty;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)caption.transform);
        }

        LateUpdate();
    }

    private void Hide()
    {
        StopPulse();
        target = null;
        captionLine = null;
        if (group != null)
            group.alpha = 0f;
        if (caption != null)
            caption.alpha = 0f;
    }

    private void StopPulse()
    {
        if (pulsing != null)
            pulsing.localScale = pulsingScale;

        pulsing = null;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (!TryGetAim(out Vector2 edge, out Vector2 towardsTarget))
        {
            Hide();
            return;
        }

        float now = Time.unscaledTime;
        group.alpha = fadeSeconds > 0f ? Mathf.Clamp01((now - shownSince) / fadeSeconds) : 1f;

        float halfLength = rect.rect.width * 0.5f;
        float bob = (0.5f - 0.5f * Mathf.Cos(now * bobSpeed)) * bobDistance;
        rect.localPosition = edge - towardsTarget * (gap + halfLength + bob);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(towardsTarget.y, towardsTarget.x) * Mathf.Rad2Deg);

        if (pulsing != null)
            pulsing.localScale = pulsingScale * (1f + pulseScale * (0.5f - 0.5f * Mathf.Cos(now * pulseSpeed)));

        PlaceCaption(edge, towardsTarget);
    }

    // Past the arrow's tail at the farthest of its bob, the way it points from (above an arrow
    // pointing down, and so on), kept inside its parent.
    private void PlaceCaption(Vector2 edge, Vector2 towardsTarget)
    {
        if (caption == null)
            return;

        if (captionLine == null || !(caption.transform is RectTransform box) || !(box.parent is RectTransform boxParent)
            || !(rect.parent is RectTransform parent))
        {
            caption.alpha = 0f;
            return;
        }

        caption.alpha = group.alpha;

        Vector2 tail = edge - towardsTarget * (gap + rect.rect.width + bobDistance);
        Vector2 tailInBoxParent = boxParent.InverseTransformPoint(parent.TransformPoint(tail));
        Vector2 size = box.rect.size;
        float halfAlong = Mathf.Abs(towardsTarget.x) > Mathf.Abs(towardsTarget.y) ? size.x * 0.5f : size.y * 0.5f;
        Vector2 center = tailInBoxParent - towardsTarget * (captionGap + halfAlong);

        Rect area = boxParent.rect;
        center.x = Mathf.Clamp(center.x, area.xMin + size.x * 0.5f, Mathf.Max(area.xMin + size.x * 0.5f, area.xMax - size.x * 0.5f));
        center.y = Mathf.Clamp(center.y, area.yMin + size.y * 0.5f, Mathf.Max(area.yMin + size.y * 0.5f, area.yMax - size.y * 0.5f));
        box.localPosition = center + Vector2.Scale(box.pivot - new Vector2(0.5f, 0.5f), size);
    }

    // The middle of the side of the target the arrow stands at, and the way it points, in the arrow's
    // parent. False while it shows nothing.
    private bool TryGetAim(out Vector2 edge, out Vector2 towardsTarget)
    {
        edge = default;
        towardsTarget = default;
        if (target == null || !target.gameObject.activeInHierarchy || !(rect.parent is RectTransform parent))
            return false;

        // Where the target is, in the pointer's parent.
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 min = parent.InverseTransformPoint(corners[0]);
        Vector2 max = min;
        for (int i = 1; i < 4; i++)
        {
            Vector2 corner = parent.InverseTransformPoint(corners[i]);
            min = Vector2.Min(min, corner);
            max = Vector2.Max(max, corner);
        }

        Vector2 center = (min + max) * 0.5f;
        towardsTarget = TowardsTarget(target.TransformPoint(target.rect.center));
        edge = towardsTarget.y < 0f ? new Vector2(center.x, max.y)
            : towardsTarget.y > 0f ? new Vector2(center.x, min.y)
            : towardsTarget.x > 0f ? new Vector2(min.x, center.y)
            : new Vector2(max.x, center.y);
        return true;
    }

    // The way the arrow points: down at a target it stands above, and so on.
    private Vector2 TowardsTarget(Vector3 targetWorldCenter)
    {
        switch (side)
        {
            case PointerSide.Above:
                return Vector2.down;
            case PointerSide.Below:
                return Vector2.up;
            case PointerSide.Left:
                return Vector2.right;
            case PointerSide.Right:
                return Vector2.left;
        }

        RectTransform canvasRect = RootCanvas != null ? RootCanvas.transform as RectTransform : null;
        if (canvasRect == null)
            return Vector2.down;

        Vector2 inCanvas = canvasRect.InverseTransformPoint(targetWorldCenter);
        return inCanvas.y < canvasRect.rect.center.y ? Vector2.down : Vector2.up;
    }
}
