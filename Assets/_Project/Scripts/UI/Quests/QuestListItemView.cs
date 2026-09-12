using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestListItemView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text unreadDotText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new(0.59607846f, 0.48235294f, 0.44313726f, 0.78f);
    [SerializeField] private Color selectedColor = new(0.27450982f, 0.46666667f, 0.59607846f, 1f);
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color selectedTextColor = Color.white;
    [SerializeField] private Color unreadDotColor = new(0.9529412f, 0.78431374f, 0.427451f, 1f);

    private int index;
    private Action<int> onSelected;

    public void Setup(QuestData quest, int itemIndex, bool selected, bool unread, Action<int> selectedCallback)
    {
        index = itemIndex;
        onSelected = selectedCallback;
        EnsureUnreadDot();

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
        }

        SetSelected(selected);
        SetUnread(unread);
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;

        if (titleText != null)
            titleText.color = selected ? selectedTextColor : normalTextColor;
    }

    public void SetUnread(bool unread)
    {
        EnsureUnreadDot();

        if (unreadDotText != null)
            unreadDotText.gameObject.SetActive(unread);
    }

    private void EnsureUnreadDot()
    {
        if (unreadDotText != null)
            return;

        GameObject dotObject = new("UnreadDot");
        dotObject.transform.SetParent(transform, false);
        dotObject.transform.SetAsFirstSibling();

        unreadDotText = dotObject.AddComponent<TextMeshProUGUI>();
        unreadDotText.text = "\u2022";
        unreadDotText.fontSize = 34f;
        unreadDotText.alignment = TextAlignmentOptions.Center;
        unreadDotText.color = unreadDotColor;
        unreadDotText.raycastTarget = false;

        RectTransform rect = unreadDotText.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(18f, 36f);

        LayoutElement layoutElement = dotObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = 18f;
        layoutElement.preferredWidth = 18f;
        layoutElement.flexibleWidth = 0f;
    }

    private void Select()
    {
        onSelected?.Invoke(index);
    }
}
