using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The bubble a fighter says a line of BattleTalk in: over their head (above their intent), or, for a
// line about something the pointer shows, right beside it, just past the pointer's tail, e.g. Dad's
// words on strength over the strength counter. In the hierarchy of the battle canvas; BattleTalk
// fills it. The continue mark shows while it waits for a click.
[RequireComponent(typeof(CanvasGroup))]
public sealed class BattleTalkBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private GameObject continueMark;
    [Tooltip("How far over the top of the speaker's sprite its bottom is, in canvas units: room for the intent under it.")]
    [SerializeField] private float aboveSprite = 70f;
    [Tooltip("How far past the pointer's tail it stands when it says what the pointer shows, in canvas units.")]
    [SerializeField] private float pastPointer = 8f;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.15f;

    private const float ScreenMargin = 16f;

    private RectTransform rect;
    private CanvasGroup group;
    private Canvas canvas;
    private Character speaker;
    private bool besidePointer;
    private float shownSince;

    public bool IsShown => speaker != null;

    private void Awake()
    {
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = 0f;
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        canvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
    }

    // Beside Pointer: it stands by what the pointer shows, if it shows something; else over the speaker.
    public void Show(Character who, string line, bool waitsForClick, bool besidePointer = false)
    {
        speaker = who;
        this.besidePointer = besidePointer;
        shownSince = Time.unscaledTime;
        if (text != null)
            text.text = line;
        if (continueMark != null)
            continueMark.SetActive(waitsForClick);

        transform.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        LateUpdate();
    }

    public void Hide()
    {
        speaker = null;
        besidePointer = false;
        if (group != null)
            group.alpha = 0f;
    }

    private void LateUpdate()
    {
        if (speaker == null)
            return;

        group.alpha = fadeSeconds > 0f ? Mathf.Clamp01((Time.unscaledTime - shownSince) / fadeSeconds) : 1f;

        if (!(rect.parent is RectTransform parent))
            return;

        if (besidePointer && TryBesidePointer(parent, out Vector2 local))
            rect.localPosition = KeptOnScreen(parent, local, keepY: true);
        else if (TryOverSpeaker(parent, out local))
            rect.localPosition = KeptOnScreen(parent, local, keepY: false);
    }

    // Past the pointer's tail, the way it points from: above an arrow pointing down, and so on.
    private bool TryBesidePointer(RectTransform parent, out Vector2 local)
    {
        local = default;
        if (canvas == null || !UiPointer.TryGetTail(canvas, out Vector3 tailWorld, out Vector2 towards))
            return false;

        Vector2 tail = parent.InverseTransformPoint(tailWorld);
        Vector2 size = rect.rect.size;
        float halfAlong = Mathf.Abs(towards.x) > Mathf.Abs(towards.y) ? size.x * 0.5f : size.y * 0.5f;
        Vector2 center = tail - towards * (pastPointer + halfAlong);
        local = center + Vector2.Scale(rect.pivot - new Vector2(0.5f, 0.5f), size);
        return true;
    }

    private bool TryOverSpeaker(RectTransform parent, out Vector2 local)
    {
        local = default;
        Camera camera = Camera.main;
        if (camera == null || canvas == null || !speaker.TryGetComponent(out SpriteRenderer sprite))
            return false;

        Bounds bounds = sprite.bounds;
        Vector3 screenPoint = camera.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, uiCamera, out local))
            return false;

        local.y += aboveSprite + rect.pivot.y * rect.rect.height;
        return true;
    }

    // Kept on screen beside someone or something at its edge, e.g. the strength counter in the corner.
    private Vector2 KeptOnScreen(RectTransform parent, Vector2 local, bool keepY)
    {
        Rect area = parent.rect;
        Vector2 size = rect.rect.size;
        Vector2 pivot = rect.pivot;

        float left = area.xMin + pivot.x * size.x + ScreenMargin;
        float right = area.xMax - (1f - pivot.x) * size.x - ScreenMargin;
        if (left < right)
            local.x = Mathf.Clamp(local.x, left, right);

        float bottom = area.yMin + pivot.y * size.y + ScreenMargin;
        float top = area.yMax - (1f - pivot.y) * size.y - ScreenMargin;
        if (keepY && bottom < top)
            local.y = Mathf.Clamp(local.y, bottom, top);

        return local;
    }
}
