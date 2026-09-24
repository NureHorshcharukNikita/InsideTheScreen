using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MainMenuSaveSlotRow : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image modePanel;
    [SerializeField, Range(0f, 1f)] private float disabledPanelBrightness = 0.68f;
    [SerializeField, Range(0f, 1f)] private float disabledPanelAlpha = 0.78f;

    private bool isSelected;
    private Texture2D previewTexture;
    private Sprite previewSprite;

    public Button Button => button;

    private void Reset()
    {
        button = GetComponent<Button>();
        label = GetComponentInChildren<TMP_Text>();
    }

    public void SetText(string text)
    {
        if (label != null)
            label.text = text;
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;

        ApplyPanelAlpha(false);
    }

    public void SetPanelSprites(Sprite backgroundSprite, Sprite highlightedSprite, Sprite modeSprite)
    {
        if (button == null || button.image == null)
            return;

        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState
        {
            highlightedSprite = highlightedSprite,
            pressedSprite = highlightedSprite,
            selectedSprite = highlightedSprite,
            disabledSprite = backgroundSprite
        };
        button.image.overrideSprite = null;
        button.image.sprite = backgroundSprite;

        ReleasePreview();
        if (modePanel != null)
        {
            modePanel.preserveAspect = false;
            modePanel.sprite = modeSprite;
        }

        ApplyPanelAlpha(false);
    }

    public void SetPreview(Texture2D texture)
    {
        if (texture == null)
            return;

        if (modePanel == null)
        {
            Destroy(texture);
            return;
        }

        previewTexture = texture;
        previewTexture.wrapMode = TextureWrapMode.Clamp;
        previewTexture.filterMode = FilterMode.Bilinear;
        previewSprite = Sprite.Create(
            previewTexture,
            new Rect(0f, 0f, previewTexture.width, previewTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        modePanel.sprite = previewSprite;
        modePanel.preserveAspect = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.interactable)
            ApplyPanelAlpha(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyPanelAlpha(isSelected);
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        ApplyPanelAlpha(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        ApplyPanelAlpha(false);
    }

    private void OnDisable()
    {
        isSelected = false;
        ApplyPanelAlpha(false);
    }

    private void OnDestroy()
    {
        ReleasePreview();
    }

    private void ApplyPanelAlpha(bool highlighted)
    {
        if (button == null || button.image == null)
            return;

        if (highlighted || button.interactable)
        {
            button.image.color = Color.white;
            return;
        }

        button.image.color = new Color(
            disabledPanelBrightness,
            disabledPanelBrightness,
            disabledPanelBrightness,
            disabledPanelAlpha);
    }

    private void ReleasePreview()
    {
        if (previewSprite != null)
            Destroy(previewSprite);
        if (previewTexture != null)
            Destroy(previewTexture);

        previewSprite = null;
        previewTexture = null;
    }
}
