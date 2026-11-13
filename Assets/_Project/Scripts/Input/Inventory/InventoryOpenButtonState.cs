using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class InventoryOpenButtonState : MonoBehaviour
{
    [SerializeField] private Button button;
    [Tooltip("The button stays hidden until this story flag is set, e.g. a1_home_intro: no backpack at home. Empty = always shown.")]
    [SerializeField, StoryFlag] private string visibleAfterStoryFlag;
    [Tooltip("The button's picture, which changes once Haru has his bags.")]
    [SerializeField] private Image icon;
    [Tooltip("Shown on it until Icon Changes At is set, e.g. Haru's coat pocket before Garr gives him the pack. Empty = always its own sprite.")]
    [SerializeField] private Sprite iconBefore;
    [SerializeField, StoryFlag] private string iconChangesAt;

    private CanvasGroup canvasGroup;
    // Its own sprite, e.g. the backpack.
    private Sprite iconAfter;

    private void Reset()
    {
        ResolveButton();
    }

    private void Awake()
    {
        ResolveButton();
        if (icon != null)
            iconAfter = icon.sprite;

        Refresh();
    }

    private void OnEnable()
    {
        ResolveButton();
        Refresh();
    }

    // After the Updates, so the button is already gone on the frame a dialogue starts.
    private void LateUpdate()
    {
        Refresh();
    }

    private void ResolveButton()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void Refresh()
    {
        // Hidden outright, not greyed out, while a dialogue or a story scene plays.
        bool visible = (string.IsNullOrWhiteSpace(visibleAfterStoryFlag) || StoryFlagManager.HasFlag(visibleAfterStoryFlag))
            && !GameStateManager.IsDialogue;
        SetVisible(visible);

        if (button != null)
            button.interactable = visible && GameStateManager.CanOpenInventory();

        if (icon != null && iconBefore != null && iconAfter != null)
        {
            bool changed = string.IsNullOrWhiteSpace(iconChangesAt) || StoryFlagManager.HasFlag(iconChangesAt);
            Sprite wanted = changed ? iconAfter : iconBefore;
            if (icon.sprite != wanted)
                icon.sprite = wanted;
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            if (visible)
                return;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
    }
}
