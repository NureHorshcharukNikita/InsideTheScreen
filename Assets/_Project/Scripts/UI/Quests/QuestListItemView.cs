using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class QuestListItemView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private Image unreadIndicator;
    [SerializeField] private Sprite selectedBackgroundSprite;

    [Header("Colors")]
    [SerializeField] private Color hoverColor = new(0.62f, 0.62f, 0.62f, 1f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color completedColor = new(0.82f, 0.78f, 0.69f, 1f);
    [SerializeField] private Color completedSelectedColor = Color.white;
    [SerializeField] private Color failedColor = new(0.78f, 0.55f, 0.52f, 1f);
    [SerializeField] private Color failedSelectedColor = Color.white;
    [SerializeField] private Color normalTextColor = new(0.96f, 0.91f, 0.84f, 1f);
    [SerializeField] private Color selectedTextColor = Color.white;
    [SerializeField] private Color completedTextColor = new(0.68f, 0.64f, 0.58f, 1f);

    private int index;
    private Action<int> onSelected;
    private QuestStatus currentStatus;
    private bool currentSelected;
    private bool pointerOver;
    private Sprite normalBackgroundSprite;

    private void Awake()
    {
        if (backgroundImage != null)
            normalBackgroundSprite = backgroundImage.sprite;

        if (button != null)
            button.transition = Selectable.Transition.None;
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
            rewardIcon.sprite = quest != null ? quest.RewardIcon : null;
            rewardIcon.enabled = rewardIcon.sprite != null;
            rewardIcon.color = Color.white;
        }

        SetStatus(status);
        SetSelected(selected);
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

        if (backgroundImage != null)
        {
            backgroundImage.sprite = currentSelected && selectedBackgroundSprite != null
                ? selectedBackgroundSprite
                : normalBackgroundSprite;

            if (currentSelected)
                backgroundImage.color = completed
                    ? completedSelectedColor
                    : failed
                        ? failedSelectedColor
                        : selectedColor;
            else if (pointerOver)
                backgroundImage.color = hoverColor;
            else if (completed)
                backgroundImage.color = completedColor;
            else if (failed)
                backgroundImage.color = failedColor;
            else
                backgroundImage.color = normalColor;
        }

        if (titleText != null)
        {
            if (completed)
                titleText.color = completedTextColor;
            else if (failed)
                titleText.color = currentSelected ? selectedTextColor : new Color(1f, 0.72f, 0.68f, 1f);
            else
                titleText.color = currentSelected ? selectedTextColor : normalTextColor;
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
