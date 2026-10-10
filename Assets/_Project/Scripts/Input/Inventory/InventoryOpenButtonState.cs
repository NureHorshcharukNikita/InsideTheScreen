using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class InventoryOpenButtonState : MonoBehaviour
{
    [SerializeField] private Button button;
    [Tooltip("The button stays hidden until this story flag is set, e.g. a1_home_intro: no backpack at home. Empty = always shown.")]
    [SerializeField, StoryFlag] private string visibleAfterStoryFlag;

    private CanvasGroup canvasGroup;

    private void Reset()
    {
        ResolveButton();
    }

    private void Awake()
    {
        ResolveButton();
        Refresh();
    }

    private void OnEnable()
    {
        ResolveButton();
        Refresh();
    }

    private void Update()
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
        bool visible = string.IsNullOrWhiteSpace(visibleAfterStoryFlag) || StoryFlagManager.HasFlag(visibleAfterStoryFlag);
        SetVisible(visible);

        if (button != null)
            button.interactable = visible && GameStateManager.CanOpenInventory();
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
