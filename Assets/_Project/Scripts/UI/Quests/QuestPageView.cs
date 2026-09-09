using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestPageView : MonoBehaviour
{
    private const int NoSelectionIndex = -1;
    private const string EmptyDetailsMessage = "Select a quest to view details";
    private const string EmptyListMessage = "No quests yet";

    [SerializeField, HideInInspector] private int hierarchyVersion;
    [SerializeField] private List<QuestData> quests = new();

    [Header("List")]
    [SerializeField] private Transform listContent;
    [SerializeField] private QuestListItemView listItemTemplate;
    [SerializeField] private ScrollRect listScroll;

    [Header("Details")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text giverText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject detailsContentRoot;
    [SerializeField] private GameObject rewardRoot;
    [SerializeField] private TMP_Text rewardLabelText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private ScrollRect detailsScroll;
    [SerializeField] private TMP_Text emptyDetailsText;
    [SerializeField] private TMP_Text emptyListText;

    private readonly List<QuestListItemView> itemViews = new();
    private int selectedIndex = NoSelectionIndex;

    private void OnEnable()
    {
        selectedIndex = NoSelectionIndex;
        BuildList();
    }

    public void SetQuests(IReadOnlyList<QuestData> newQuests)
    {
        quests.Clear();

        if (newQuests != null)
        {
            for (int i = 0; i < newQuests.Count; i++)
            {
                if (newQuests[i] != null)
                    quests.Add(newQuests[i]);
            }
        }

        selectedIndex = NoSelectionIndex;
        BuildList();
    }

    private void BuildList()
    {
        if (listContent == null || listItemTemplate == null)
        {
            ClearDetails();
            SetDetailsContentVisible(false);
            SetEmptyDetailsVisible(false);
            SetEmptyListVisible(false);
            return;
        }

        if (quests.Count == 0)
        {
            HideListItems();
            SetListContentVisible(false);
            SetEmptyListVisible(true);
            ShowEmptyDetails();
            ResetScrolls();
            return;
        }

        SetListContentVisible(true);
        SetEmptyListVisible(false);

        if (selectedIndex >= quests.Count)
            selectedIndex = NoSelectionIndex;

        List<QuestListItemView> reusableItems = GetReusableListItems();
        itemViews.Clear();

        for (int i = 0; i < quests.Count; i++)
        {
            QuestListItemView item = i < reusableItems.Count
                ? reusableItems[i]
                : Instantiate(listItemTemplate, listContent);

            item.gameObject.SetActive(true);
            item.Setup(quests[i], i, i == selectedIndex, SelectQuest);
            itemViews.Add(item);
        }

        for (int i = quests.Count; i < reusableItems.Count; i++)
            reusableItems[i].gameObject.SetActive(false);

        if (selectedIndex == NoSelectionIndex)
            ShowEmptyDetails();
        else
            ShowQuest(selectedIndex);

        ResetScrolls();
    }

    private void SelectQuest(int index)
    {
        if (index < 0 || index >= quests.Count)
            return;

        selectedIndex = index;
        ShowQuest(selectedIndex);

        for (int i = 0; i < itemViews.Count; i++)
            itemViews[i].SetSelected(i == selectedIndex);
    }

    private void ShowQuest(int index)
    {
        QuestData quest = quests[index];

        SetDetailsContentVisible(true);
        SetEmptyDetailsVisible(false);

        if (titleText != null)
            titleText.text = quest.Title;

        if (giverText != null)
            giverText.text = string.IsNullOrWhiteSpace(quest.GiverName) ? string.Empty : $"Giver: {quest.GiverName}";

        if (descriptionText != null)
            descriptionText.text = quest.Description;

        bool hasReward = quest.RewardIcon != null || !string.IsNullOrWhiteSpace(quest.RewardLabel);
        if (rewardRoot != null)
            rewardRoot.SetActive(hasReward);

        if (rewardLabelText != null)
            rewardLabelText.text = quest.RewardLabel;

        if (rewardIcon != null)
        {
            rewardIcon.sprite = quest.RewardIcon;
            rewardIcon.enabled = quest.RewardIcon != null;
        }

        ResetScrolls();
    }

    private void ShowEmptyDetails()
    {
        ClearDetails();
        SetDetailsContentVisible(false);
        SetEmptyDetailsVisible(true);
    }

    private void ClearDetails()
    {
        if (titleText != null)
            titleText.text = string.Empty;

        if (giverText != null)
            giverText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (rewardRoot != null)
            rewardRoot.SetActive(false);
    }

    private void SetDetailsContentVisible(bool visible)
    {
        GameObject contentRoot = detailsContentRoot;
        if (contentRoot == null && detailsScroll != null && detailsScroll.content != null)
            contentRoot = detailsScroll.content.gameObject;

        if (contentRoot != null)
            contentRoot.SetActive(visible);
    }

    private void SetEmptyDetailsVisible(bool visible)
    {
        if (emptyDetailsText == null)
            return;

        emptyDetailsText.gameObject.SetActive(visible);
        emptyDetailsText.text = EmptyDetailsMessage;
    }

    private void SetEmptyListVisible(bool visible)
    {
        if (emptyListText == null)
            return;

        emptyListText.gameObject.SetActive(visible);
        emptyListText.text = EmptyListMessage;
    }

    private void SetListContentVisible(bool visible)
    {
        if (listContent != null)
            listContent.gameObject.SetActive(visible);
    }

    private List<QuestListItemView> GetReusableListItems()
    {
        List<QuestListItemView> reusableItems = new();

        if (listContent == null)
            return reusableItems;

        for (int i = 0; i < listContent.childCount; i++)
        {
            Transform child = listContent.GetChild(i);
            if (listItemTemplate != null && child == listItemTemplate.transform)
                continue;

            if (child.TryGetComponent(out QuestListItemView item))
                reusableItems.Add(item);
        }

        return reusableItems;
    }

    private void HideListItems()
    {
        itemViews.Clear();

        if (listContent == null)
            return;

        for (int i = 0; i < listContent.childCount; i++)
        {
            Transform child = listContent.GetChild(i);
            if (listItemTemplate != null && child == listItemTemplate.transform)
                continue;

            child.gameObject.SetActive(false);
        }
    }

    private void ResetScrolls()
    {
        if (detailsScroll != null)
            detailsScroll.verticalNormalizedPosition = 1f;

        if (listScroll != null)
            listScroll.verticalNormalizedPosition = 1f;
    }
}
