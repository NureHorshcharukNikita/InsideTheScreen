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

    [Header("Colors")]
    [SerializeField] private Color normalColor = new(0.59607846f, 0.48235294f, 0.44313726f, 0.78f);
    [SerializeField] private Color selectedColor = new(0.27450982f, 0.46666667f, 0.59607846f, 1f);
    [SerializeField] private Color normalTextColor = Color.black;
    [SerializeField] private Color selectedTextColor = Color.white;

    private int index;
    private Action<int> onSelected;

    public void Setup(QuestData quest, int itemIndex, bool selected, Action<int> selectedCallback)
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
        }

        SetSelected(selected);
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;

        if (titleText != null)
            titleText.color = selected ? selectedTextColor : normalTextColor;
    }

    private void Select()
    {
        onSelected?.Invoke(index);
    }
}
