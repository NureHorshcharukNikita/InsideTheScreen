using System.Collections.Generic;
using UnityEngine;

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

    private RectTransform rect;
    private CanvasGroup group;
    private Canvas rootCanvas;
    private RectTransform target;
    private PointerSide side;
    private Transform pulsing;
    private Vector3 pulsingScale;
    private float shownSince;

    private void Awake()
    {
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;
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

    // Points at the target with this id (UiPointerTarget) on whichever canvas it is.
    public static bool TryPointAt(string id, PointerSide side = PointerSide.Auto)
    {
        if (!UiPointerTarget.TryFind(id, out UiPointerTarget found))
            return false;

        return TryPointAt((RectTransform)found.transform, side, found.Pulse);
    }

    public static bool TryPointAt(RectTransform target, PointerSide side = PointerSide.Auto, bool pulse = false)
    {
        if (target == null)
            return false;

        Canvas canvas = target.GetComponentInParent<Canvas>();
        Canvas root = canvas != null ? canvas.rootCanvas : null;
        foreach (UiPointer pointer in All)
        {
            if (pointer != null && pointer.RootCanvas == root)
            {
                pointer.Show(target, side, pulse);
                return true;
            }
        }

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

    private void Show(RectTransform newTarget, PointerSide newSide, bool pulse)
    {
        if (newTarget == target && newSide == side)
            return;

        StopPulse();
        if (target == null)
            shownSince = Time.unscaledTime;

        target = newTarget;
        side = newSide;
        if (pulse)
        {
            pulsing = target;
            pulsingScale = target.localScale;
        }

        LateUpdate();
    }

    private void Hide()
    {
        StopPulse();
        target = null;
        if (group != null)
            group.alpha = 0f;
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

        if (!target.gameObject.activeInHierarchy || !(rect.parent is RectTransform parent))
        {
            Hide();
            return;
        }

        float now = Time.unscaledTime;
        group.alpha = fadeSeconds > 0f ? Mathf.Clamp01((now - shownSince) / fadeSeconds) : 1f;

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
        Vector2 towardsTarget = TowardsTarget(target.TransformPoint(target.rect.center));
        float halfLength = rect.rect.width * 0.5f;
        float bob = (0.5f - 0.5f * Mathf.Cos(now * bobSpeed)) * bobDistance;

        Vector2 edge = towardsTarget.y < 0f ? new Vector2(center.x, max.y)
            : towardsTarget.y > 0f ? new Vector2(center.x, min.y)
            : towardsTarget.x > 0f ? new Vector2(min.x, center.y)
            : new Vector2(max.x, center.y);

        rect.localPosition = edge - towardsTarget * (gap + halfLength + bob);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(towardsTarget.y, towardsTarget.x) * Mathf.Rad2Deg);

        if (pulsing != null)
            pulsing.localScale = pulsingScale * (1f + pulseScale * (0.5f - 0.5f * Mathf.Cos(now * pulseSpeed)));
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
