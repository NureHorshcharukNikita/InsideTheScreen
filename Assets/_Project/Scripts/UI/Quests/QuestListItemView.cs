using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One quest in the quest log's list. Three things, each with a look of its own, so none is taken for
// another (a finished quest used to be dimmed just like a hovered one):
// - the pointer on it: the Hover Glow comes up over the background, the title goes white;
// - the quest picked: the gold frame, the title white;
// - a finished quest: its mark (a tick, a cross when failed) where the reward was, the title grey or
//   reddish, the whole row faded a little. Picked, it is not faded.
// The background keeps the colour it has in the hierarchy; only its sprite changes when picked.
public sealed class QuestListItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private Image unreadIndicator;
    [SerializeField] private Sprite selectedBackgroundSprite;

    [Header("Hover")]
    [Tooltip("Comes up over the background while the pointer is on the row, unless it is the one picked. Empty = no hover look.")]
    [SerializeField] private GameObject hoverGlow;

    [Header("Finished quests")]
    [Tooltip("Where a finished quest shows its mark; the reward icon is gone then, the reward is paid.")]
    [SerializeField] private Image statusMark;
    [SerializeField] private Sprite completedMark;
    [Tooltip("Empty = a failed quest shows no mark, only its reddish title.")]
    [SerializeField] private Sprite failedMark;
    [Tooltip("Fades a finished quest's row to Finished Alpha, unless it is the one picked. Empty = not faded.")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Range(0f, 1f)] private float finishedAlpha = 0.72f;

    [Header("Title colors")]
    [SerializeField] private Color normalTextColor = new(0.96f, 0.91f, 0.84f, 1f);
    [SerializeField] private Color hoverTextColor = Color.white;
    [SerializeField] private Color selectedTextColor = Color.white;
    [SerializeField] private Color completedTextColor = new(0.68f, 0.64f, 0.58f, 1f);
    [SerializeField] private Color failedTextColor = new(0.86f, 0.52f, 0.47f, 1f);

    private int index;
    private Action<int> onSelected;
    private QuestStatus currentStatus;
    private bool currentSelected;
    private bool pointerOver;
    private bool hasReward;
    private Sprite normalBackgroundSprite;

    private void Awake()
    {
        if (backgroundImage != null)
            normalBackgroundSprite = backgroundImage.sprite;

        if (button != null)
            button.transition = Selectable.Transition.None;
    }

    // A row put away for the next list keeps no hover from where the pointer last was.
    private void OnDisable()
    {
        pointerOver = false;
        if (hoverGlow != null)
            hoverGlow.SetActive(false);
    }

    public void Setup(
        QuestData quest,
        int itemIndex,
        bool selected,
        bool unread,
        QuestStatus status,
        Action<int> selectedCallback)
    {
        index = itemIndex;
        onSelected = selectedCallback;

        if (button != null)
        {
            button.onClick.RemoveListener(Select);
            button.onClick.AddListener(Select);
        }

        if (titleText != null)
            titleText.text = quest != null ? quest.Title : string.Empty;

        if (rewardIcon != null)
        {
            rewardIcon.sprite = quest != null ? quest.ShownRewardIcon : null;
            rewardIcon.color = Color.white;
            hasReward = rewardIcon.sprite != null;
        }

        currentStatus = status;
        currentSelected = selected;
        ApplyVisualState();
        SetUnread(unread);
    }

    public void SetSelected(bool selected)
    {
        currentSelected = selected;
        ApplyVisualState();
    }

    public void SetStatus(QuestStatus status)
    {
        currentStatus = status;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        bool completed = currentStatus == QuestStatus.Completed;
        bool failed = currentStatus == QuestStatus.Failed;
        bool finished = completed || failed;

        if (backgroundImage != null)
            backgroundImage.sprite = currentSelected && selectedBackgroundSprite != null
                ? selectedBackgroundSprite
                : normalBackgroundSprite;

        if (hoverGlow != null)
            hoverGlow.SetActive(pointerOver && !currentSelected);

        Sprite mark = completed ? completedMark : failed ? failedMark : null;
        if (statusMark != null)
        {
            statusMark.sprite = mark;
            statusMark.gameObject.SetActive(mark != null);
        }

        // The reward makes way for the mark; with no Status Mark set up, it stays as before.
        if (rewardIcon != null)
            rewardIcon.enabled = hasReward && (statusMark == null || !finished);

        if (canvasGroup != null)
            canvasGroup.alpha = finished && !currentSelected ? finishedAlpha : 1f;

        if (titleText != null)
        {
            titleText.color = currentSelected ? selectedTextColor
                : completed ? completedTextColor
                : failed ? failedTextColor
                : pointerOver ? hoverTextColor
                : normalTextColor;
        }
    }

    public void SetUnread(bool unread)
    {
        if (unreadIndicator != null)
            unreadIndicator.gameObject.SetActive(unread);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
        ApplyVisualState();
    }

    private void Select()
    {
        onSelected?.Invoke(index);
    }
}
