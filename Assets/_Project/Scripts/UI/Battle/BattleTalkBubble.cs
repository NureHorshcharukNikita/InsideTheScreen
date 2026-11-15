using TMPro;
using UnityEngine;

// The bubble a fighter says a line of BattleTalk in, over their head (above their intent). In the
// hierarchy of the battle canvas; BattleTalk fills it. The continue mark shows while it waits for a
// click.
[RequireComponent(typeof(CanvasGroup))]
public sealed class BattleTalkBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private GameObject continueMark;
    [Tooltip("How far over the top of the speaker's sprite its bottom is, in canvas units: room for the intent under it.")]
    [SerializeField] private float aboveSprite = 70f;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.15f;

    private RectTransform rect;
    private CanvasGroup group;
    private Canvas canvas;
    private Character speaker;
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

    public void Show(Character who, string line, bool waitsForClick)
    {
        speaker = who;
        shownSince = Time.unscaledTime;
        if (text != null)
            text.text = line;
        if (continueMark != null)
            continueMark.SetActive(waitsForClick);

        transform.SetAsLastSibling();
        LateUpdate();
    }

    public void Hide()
    {
        speaker = null;
        if (group != null)
            group.alpha = 0f;
    }

    private void LateUpdate()
    {
        if (speaker == null)
            return;

        group.alpha = fadeSeconds > 0f ? Mathf.Clamp01((Time.unscaledTime - shownSince) / fadeSeconds) : 1f;

        Camera camera = Camera.main;
        if (camera == null || canvas == null || !speaker.TryGetComponent(out SpriteRenderer sprite))
            return;

        Bounds bounds = sprite.bounds;
        Vector3 screenPoint = camera.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!(rect.parent is RectTransform parent)
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, uiCamera, out Vector2 local))
            return;

        local.y += aboveSprite + rect.pivot.y * rect.rect.height;

        // Kept on screen beside someone standing at its edge.
        const float margin = 16f;
        Rect area = parent.rect;
        float left = area.xMin + rect.pivot.x * rect.rect.width + margin;
        float right = area.xMax - (1f - rect.pivot.x) * rect.rect.width - margin;
        local.x = left < right ? Mathf.Clamp(local.x, left, right) : local.x;
        rect.localPosition = local;
    }
}
