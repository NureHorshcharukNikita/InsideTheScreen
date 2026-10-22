using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class QuestPageView : MonoBehaviour
{
    private const int NoSelectionIndex = -1;
    private const string EmptyDetailsMessage = "Select a quest to view details";
    private const string EmptyListMessage = "No quests yet";
    private const string RewardItemName = "RewardItem";
    private static readonly Color DetailsTextColor = new(0.96f, 0.91f, 0.84f, 1f);
    private static readonly Color CompletedObjectiveColor = new(0.62f, 0.58f, 0.53f, 1f);

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
    [SerializeField] private Image statusBackground;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject objectivesRoot;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private Image objectiveCheckbox;
    [SerializeField] private GameObject detailsContentRoot;
    [SerializeField] private GameObject rewardsRoot;
    [SerializeField] private Transform rewardsContainer;
    [SerializeField] private QuestRewardItemView rewardItemTemplate;
    [SerializeField] private ScrollRect detailsScroll;
    [SerializeField] private TMP_Text emptyDetailsText;
    [SerializeField] private TMP_Text emptyListText;

    [Header("Quest Art")]
    [SerializeField] private Sprite availableStatusSprite;
    [SerializeField] private Sprite activeStatusSprite;
    [SerializeField] private Sprite completedStatusSprite;
    [SerializeField] private Sprite failedStatusSprite;
    [SerializeField] private Sprite uncheckedObjectiveSprite;
    [SerializeField] private Sprite checkedObjectiveSprite;

    private readonly List<QuestListItemView> itemViews = new();
    private readonly List<QuestRewardItemView> rewardItemViews = new();
    private readonly List<QuestData> receivedQuests = new();
    private int selectedIndex = NoSelectionIndex;

    private void OnEnable()
    {
        QuestManager.QuestChanged += HandleQuestChanged;
        selectedIndex = NoSelectionIndex;
        BuildList();
    }

    private void OnDisable()
    {
        QuestManager.QuestChanged -= HandleQuestChanged;
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
        RefreshReceivedQuests();

        if (listContent == null || listItemTemplate == null)
        {
            ClearDetails();
            SetDetailsContentVisible(false);
            SetEmptyDetailsVisible(false);
            SetEmptyListVisible(false);
            return;
        }

        if (receivedQuests.Count == 0)
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

        if (selectedIndex >= receivedQuests.Count)
            selectedIndex = NoSelectionIndex;

        List<QuestListItemView> reusableItems = GetReusableListItems();
        itemViews.Clear();

        for (int i = 0; i < receivedQuests.Count; i++)
        {
            QuestListItemView item = i < reusableItems.Count
                ? reusableItems[i]
                : Instantiate(listItemTemplate, listContent);

            QuestStatus status = QuestManager.GetStatus(receivedQuests[i]);
            item.gameObject.SetActive(true);
            item.Setup(
                receivedQuests[i],
                i,
                i == selectedIndex,
                QuestManager.IsUnread(receivedQuests[i]),
                status,
                SelectQuest);
            itemViews.Add(item);
        }

        for (int i = receivedQuests.Count; i < reusableItems.Count; i++)
            reusableItems[i].gameObject.SetActive(false);

        if (selectedIndex == NoSelectionIndex)
            ShowEmptyDetails();
        else
            ShowQuest(selectedIndex);

        ResetScrolls();
    }

    private void SelectQuest(int index)
    {
        if (index < 0 || index >= receivedQuests.Count)
            return;

        selectedIndex = index;
        QuestManager.MarkOpened(receivedQuests[selectedIndex]);
        ShowQuest(selectedIndex);

        for (int i = 0; i < itemViews.Count; i++)
        {
            itemViews[i].SetSelected(i == selectedIndex);
            itemViews[i].SetUnread(QuestManager.IsUnread(receivedQuests[i]));
            itemViews[i].SetStatus(QuestManager.GetStatus(receivedQuests[i]));
        }
    }

    private void ShowQuest(int index)
    {
        QuestData quest = receivedQuests[index];
        QuestStatus status = QuestManager.GetStatus(quest);

        SetDetailsContentVisible(true);
        SetEmptyDetailsVisible(false);

        if (titleText != null)
            titleText.text = quest.Title;

        if (giverText != null)
        {
            giverText.text = string.IsNullOrWhiteSpace(quest.GiverName)
                ? string.Empty
                : $"<color=#9A948C>Giver:</color> {quest.GiverName}";
            giverText.color = DetailsTextColor;
        }

        if (descriptionText != null)
        {
            descriptionText.text = quest.Description;
            descriptionText.color = DetailsTextColor;
        }

        ApplyStatus(status);
        ApplyObjective(quest, status);

        ApplyRewards(quest);

        RebuildDetailsLayout();
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

        if (statusBackground != null)
            statusBackground.gameObject.SetActive(false);

        if (objectivesRoot != null)
            objectivesRoot.SetActive(false);

        if (rewardsRoot != null)
            rewardsRoot.SetActive(false);
        else if (rewardsContainer != null)
            rewardsContainer.gameObject.SetActive(false);
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

    private void RebuildDetailsLayout()
    {
        if (detailsScroll == null || detailsScroll.content == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(detailsScroll.content);
    }

    private void RefreshReceivedQuests()
    {
        receivedQuests.Clear();
        receivedQuests.AddRange(QuestManager.GetReceivedQuests(quests));
    }

    private void HandleQuestChanged(QuestData quest, QuestStatus status)
    {
        BuildList();
    }

    private void ApplyStatus(QuestStatus status)
    {
        if (statusBackground != null)
        {
            statusBackground.gameObject.SetActive(true);
            statusBackground.sprite = status switch
            {
                QuestStatus.Active => activeStatusSprite,
                QuestStatus.Completed => completedStatusSprite,
                QuestStatus.Failed => failedStatusSprite,
                _ => availableStatusSprite
            };
        }

        if (statusText == null)
            return;

        statusText.text = status.ToString();
        statusText.color = status switch
        {
            QuestStatus.Active => new Color(0.28f, 0.16f, 0.06f, 1f),
            QuestStatus.Completed => new Color(0.3f, 0.18f, 0.04f, 1f),
            QuestStatus.Failed => new Color(0.34f, 0.08f, 0.06f, 1f),
            _ => new Color(0.08f, 0.17f, 0.27f, 1f)
        };
    }

    private void ApplyObjective(QuestData quest, QuestStatus status)
    {
        bool completed = status == QuestStatus.Completed;

        if (objectivesRoot != null)
            objectivesRoot.SetActive(true);

        if (objectiveText != null)
        {
            objectiveText.text = QuestManager.TryGetObjectiveProgress(quest, out int current, out int target)
                ? $"{quest.ObjectiveLabel} ({current}/{target})"
                : quest.ObjectiveLabel;
            objectiveText.color = completed ? CompletedObjectiveColor : DetailsTextColor;
        }

        if (objectiveCheckbox != null)
            objectiveCheckbox.sprite = completed ? checkedObjectiveSprite : uncheckedObjectiveSprite;
    }

    private void ApplyRewards(QuestData quest)
    {
        if (rewardsContainer == null || rewardItemTemplate == null)
            return;

        int rewardCount = quest != null ? quest.RewardCount : 0;
        if (rewardsRoot != null)
            rewardsRoot.SetActive(rewardCount > 0);

        rewardsContainer.gameObject.SetActive(rewardCount > 0);
        if (rewardCount == 0)
            return;

        rewardItemTemplate.gameObject.SetActive(false);

        while (rewardItemViews.Count < rewardCount)
        {
            QuestRewardItemView item = Instantiate(rewardItemTemplate, rewardsContainer);
            item.gameObject.name = RewardItemName;
            rewardItemViews.Add(item);
        }

        for (int i = 0; i < rewardItemViews.Count; i++)
        {
            bool visible = i < rewardCount;
            rewardItemViews[i].gameObject.SetActive(visible);

            if (visible)
                rewardItemViews[i].Setup(quest.GetReward(i));
        }
    }
}
