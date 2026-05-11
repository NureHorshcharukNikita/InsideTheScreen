using TMPro;
using UnityEngine;

public partial class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Character target;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform hpFill;
    [SerializeField] private TMP_Text hpText;

    [SerializeField, Range(0f, 1f)] private float anchorHeightAlongSprite = 0f;
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
        SnapToSpriteWorldAnchor();
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
