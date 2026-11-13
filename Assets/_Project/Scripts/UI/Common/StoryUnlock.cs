using UnityEngine;

// A part of the HUD that is there only once the story has brought it up (a story flag, HudFlags), e.g.
// the crafting tab once Garr shows Haru his workbench, or the gold once the trader pays him. Until
// then it is gone. A tab leaves no gap: the tabs left close up around the middle of their row (Keep
// Place off). A counter keeps its place empty, so nothing next to it moves (Keep Place on: its Canvas
// Group goes clear). When the story brings it up, a hint at the bottom gives the key to see it once
// the player has control again, e.g. "Press I to view your health" (InventoryInput); nothing opens by
// itself.
public sealed class StoryUnlock : MonoBehaviour
{
    [Tooltip("There once this story flag is set. Empty = always.")]
    [SerializeField, StoryFlag] private string flag;
    [Tooltip("Hidden, it keeps its place (its Canvas Group goes clear) instead of going and letting the others close up.")]
    [SerializeField] private bool keepPlace;
    [Tooltip("The hint once the story brings it up, after the key, e.g. 'to view your health'. Empty = no hint.")]
    [SerializeField] private string keyHint;
    [Tooltip("A counter: the key its hint gives (a tab's hint gives the tab's own quick key).")]
    [SerializeField] private GameInputAction hintKey = GameInputAction.Inventory;

    private CanvasGroup canvasGroup;
    private bool subscribed;

    public string KeyHint => keyHint;
    public GameInputAction HintKey => hintKey;
    public string Flag => flag;
    public bool IsUnlocked => string.IsNullOrWhiteSpace(flag) || StoryFlagManager.HasFlag(flag);

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        StoryFlagManager.Changed += Apply;
        subscribed = true;
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnDestroy()
    {
        if (subscribed)
            StoryFlagManager.Changed -= Apply;
    }

    private void Apply()
    {
        bool unlocked = IsUnlocked;
        if (keepPlace && canvasGroup != null)
        {
            canvasGroup.alpha = unlocked ? 1f : 0f;
            canvasGroup.interactable = unlocked;
            canvasGroup.blocksRaycasts = unlocked;
        }
        else if (gameObject.activeSelf != unlocked)
        {
            gameObject.SetActive(unlocked);
        }
    }
}
