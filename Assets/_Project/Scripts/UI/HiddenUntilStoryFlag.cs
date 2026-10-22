using UnityEngine;

// Keeps a piece of the HUD invisible until a story flag is set, e.g. the minimap: it appears
// together with the backpack once the home intro is over, not while the eyes open in the forest.
// Hides through a CanvasGroup rather than SetActive, so it does not fight the doorways and
// arrival points that show and hide the minimap as the player moves between areas.
public sealed class HiddenUntilStoryFlag : MonoBehaviour
{
    [Tooltip("Hidden until this story flag is set, e.g. a1_home_intro. Empty = always shown.")]
    [SerializeField, StoryFlag] private string storyFlag;

    private CanvasGroup canvasGroup;

    private void OnEnable()
    {
        StoryFlagManager.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        StoryFlagManager.Changed -= Refresh;
    }

    private void Refresh()
    {
        bool visible = string.IsNullOrWhiteSpace(storyFlag) || StoryFlagManager.HasFlag(storyFlag);

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
