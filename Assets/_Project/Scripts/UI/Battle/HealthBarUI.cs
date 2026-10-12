using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Character target;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform hpFill;
    [SerializeField] private TMP_Text hpText;

    [Header("instantEffects")]
    [SerializeField] private Vector2 effectsOffset = new(0f, -7f);

    [SerializeField] private float belowSpritePadding = 4f;

    private RectTransform _rect;
    private Canvas _canvas;

    // Where the bar was on the battle UI before DrawAmongSprites, for DrawOnUi.
    private Transform uiParent;
    private int uiSiblingIndex;
    private Canvas uiCanvas;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _canvas = GetComponentInParent<Canvas>();
        InitializeEffectsView();
    }

    public Character Target => target;

    // For HP bars made at runtime: one per extra enemy or ally.
    public void Bind(Character newTarget)
    {
        if (isActiveAndEnabled)
            Unsubscribe();

        target = newTarget;

        if (isActiveAndEnabled)
            Subscribe();
    }

    // Moves the bar onto a canvas drawn among the sprites (Screen Space - Camera), at this sorting
    // order: just above its own fighter, below the fighters standing in front of them, whose
    // bodies then hide it. spriteSortedCanvas covers the screen like the battle UI canvas.
    public void DrawAmongSprites(Canvas spriteSortedCanvas, int sortingLayerId, int sortingOrder)
    {
        if (transform.parent != spriteSortedCanvas.transform)
        {
            uiParent = transform.parent;
            uiSiblingIndex = transform.GetSiblingIndex();
            uiCanvas = _canvas;
            transform.SetParent(spriteSortedCanvas.transform, false);
        }

        if (!TryGetComponent(out Canvas ownCanvas))
            ownCanvas = gameObject.AddComponent<Canvas>();

        ownCanvas.overrideSorting = true;
        ownCanvas.sortingLayerID = sortingLayerId;
        ownCanvas.sortingOrder = sortingOrder;

        // The effect badges still show their tooltips.
        if (!TryGetComponent(out GraphicRaycaster _))
            gameObject.AddComponent<GraphicRaycaster>();

        _canvas = spriteSortedCanvas;
        SnapToSpriteWorldAnchor();
    }

    // Back where DrawAmongSprites took it from, over every sprite: for a fighter nobody stands in
    // front of any more.
    public void DrawOnUi()
    {
        if (uiParent == null || transform.parent == uiParent)
            return;

        transform.SetParent(uiParent, false);
        transform.SetSiblingIndex(uiSiblingIndex);
        if (TryGetComponent(out Canvas ownCanvas))
            ownCanvas.overrideSorting = false;

        _canvas = uiCanvas;
        SnapToSpriteWorldAnchor();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (target == null)
            return;

        target.HealthChanged += RefreshHealth;
        SubscribeEffects(target);
        RefreshHealth(target.CurrentHealth, target.MaxHealth);
        RefreshEffects();
    }

    private void Unsubscribe()
    {
        if (target == null)
            return;

        target.HealthChanged -= RefreshHealth;
        UnsubscribeEffects(target);
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
