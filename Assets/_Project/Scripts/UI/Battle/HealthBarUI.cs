using TMPro;
using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Character target;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform hpFill;
    [SerializeField] private TMP_Text hpText;

    [Header("Layout under sprite")]
    [SerializeField] private float belowSpritePadding = 4f;

    private RectTransform _rect;
    private Canvas _canvas;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        if (target == null)
            return;

        target.HealthChanged += RefreshHealth;
        RefreshHealth(target.CurrentHealth, target.MaxHealth);
    }

    private void OnDisable()
    {
        if (target == null)
            return;

        target.HealthChanged -= RefreshHealth;
    }

    private void LateUpdate()
    {
        if (target == null || _canvas == null)
            return;

        SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        Bounds bounds = spriteRenderer.bounds;
        Vector3 worldPoint = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

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

    private void RefreshHealth(int current, int max)
    {
        float percent = max > 0 ? (float)current / max : 0f;
        float fullWidth = background.rect.width;
        float hiddenWidth = fullWidth * (1f - percent);

        hpFill.offsetMax = new Vector2(-hiddenWidth, hpFill.offsetMax.y);
        hpText.text = current + " / " + max;
    }
}
