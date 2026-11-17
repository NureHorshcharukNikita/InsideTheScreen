using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The quest log: the list of quests received on the left, the one picked on the right. Over the list,
// tabs: all of them, the quests under way, the finished ones (completed or failed), each with its count.
// The list puts the quests under way first, then the finished ones, each newest first. The tab stays
// while the game runs. With no tabs set up, the list shows all quests.
public sealed class QuestPageView : MonoBehaviour
{
    private const int NoSelectionIndex = -1;
    private const string EmptyDetailsMessage = "Select a quest to view details";
    private const string EmptyListMessage = "No quests yet";
    private const string NoActiveQuestsMessage = "No active quests";
    private const string NoFinishedQuestsMessage = "No completed quests yet";
    private const string RewardItemName = "RewardItem";

    private enum ListFilter
    {
        Active,
        Finished,
        All
    }
    private static readonly Color DetailsTextColor = new(0.96f, 0.91f, 0.84f, 1f);
    private static readonly Color CompletedObjectiveColor = new(0.62f, 0.58f, 0.53f, 1f);

    [SerializeField, HideInInspector] private int hierarchyVersion;
    [SerializeField] private List<QuestData> quests = new();

    [Header("List")]
    [SerializeField] private Transform listContent;
    [SerializeField] private QuestListItemView listItemTemplate;
    [SerializeField] private ScrollRect listScroll;

    [Header("Tabs")]
    [Tooltip("The quests under way.")]
    [SerializeField] private CategoryButton activeTab;
    [Tooltip("The finished quests: completed and failed.")]
    [SerializeField] private CategoryButton finishedTab;
    [Tooltip("All quests received. The tab picked when the game starts.")]
    [SerializeField] private CategoryButton allTab;

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
    // Objective rows after the first, cloned from it: one per step reached.
    private readonly List<(GameObject row, TMP_Text label, Image box)> objectiveRows = new();
    // The quests shown: received, in the tab picked, in list order.
    private readonly List<QuestData> receivedQuests = new();
    private QuestData selectedQuest;
    private ListFilter filter = ListFilter.All;
    private int receivedCount;
    private int activeCount;

    private bool HasTabs => activeTab != null || finishedTab != null || allTab != null;
    private ListFilter ShownFilter => HasTabs ? filter : ListFilter.All;

    private void OnEnable()
    {
        QuestManager.QuestChanged += HandleQuestChanged;
        selectedQuest = null;
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

        selectedQuest = null;
        BuildList();
    }

    // From the top when the page opens, gets new quests or another tab; kept where it
    // was scrolled to when a quest changes while it is open. The quest picked stays picked while it is
    // still in the list.
    private void BuildList(bool fromTop = true)
    {
        RefreshReceivedQuests();
        RefreshTabs();

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
            selectedQuest = null;
            HideListItems();
            SetListContentVisible(false);
            SetEmptyListVisible(true);
            ShowEmptyDetails();
            ResetScrolls();
            return;
        }

        SetListContentVisible(true);
        SetEmptyListVisible(false);

        int selectedIndex = selectedQuest != null ? receivedQuests.IndexOf(selectedQuest) : NoSelectionIndex;
        if (selectedIndex == NoSelectionIndex)
            selectedQuest = null;

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

        if (fromTop)
            ResetScrolls();
    }

    private void SelectQuest(int index)
    {
        if (index < 0 || index >= receivedQuests.Count)
            return;

        selectedQuest = receivedQuests[index];
        QuestManager.MarkOpened(selectedQuest);
        ShowQuest(index);

        for (int i = 0; i < itemViews.Count; i++)
        {
            itemViews[i].SetSelected(i == index);
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

        // No giver (story quests): the line is gone, not an empty gap under the title.
        if (giverText != null)
        {
            bool hasGiver = !string.IsNullOrWhiteSpace(quest.GiverName);
            giverText.gameObject.SetActive(hasGiver);
            giverText.text = hasGiver ? $"<color=#9A948C>Giver:</color> {KnownNames.Shown(quest.GiverName)}" : string.Empty;
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
        // Only the details: the list stays where it was scrolled to, e.g. down to the quest picked.
        ResetDetailsScroll();
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
        {
            giverText.text = string.Empty;
            giverText.gameObject.SetActive(false);
        }

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

    // Why the list is empty: no quest yet at all, or none in the tab.
    private void SetEmptyListVisible(bool visible)
    {
        if (emptyListText == null)
            return;

        emptyListText.gameObject.SetActive(visible);
        emptyListText.text = receivedCount == 0 ? EmptyListMessage
            : ShownFilter switch
            {
                ListFilter.Active => NoActiveQuestsMessage,
                ListFilter.Finished => NoFinishedQuestsMessage,
                _ => EmptyListMessage
            };
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
        ResetDetailsScroll();

        if (listScroll != null)
            listScroll.verticalNormalizedPosition = 1f;
    }

    private void ResetDetailsScroll()
    {
        if (detailsScroll != null)
            detailsScroll.verticalNormalizedPosition = 1f;
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
        List<QuestData> received = QuestManager.GetReceivedQuests(quests);
        receivedCount = received.Count;
        activeCount = 0;
        foreach (QuestData quest in received)
        {
            if (!IsFinished(quest))
                activeCount++;
        }

        ListFilter shown = ShownFilter;

        // The quests under way first, then the finished ones; each newest first, as received.
        for (int pass = 0; pass < 2; pass++)
        {
            bool finishedPass = pass == 1;
            if (shown == ListFilter.Active && finishedPass || shown == ListFilter.Finished && !finishedPass)
                continue;

            foreach (QuestData quest in received)
            {
                if (IsFinished(quest) == finishedPass)
                    receivedQuests.Add(quest);
            }
        }
    }

    private static bool IsFinished(QuestData quest)
    {
        return QuestManager.GetStatus(quest) is QuestStatus.Completed or QuestStatus.Failed;
    }

    private void RefreshTabs()
    {
        SetTab(activeTab, ListFilter.Active, $"Active ({activeCount})");
        SetTab(finishedTab, ListFilter.Finished, $"Completed ({receivedCount - activeCount})");
        SetTab(allTab, ListFilter.All, $"All ({receivedCount})");
    }

    private void SetTab(CategoryButton tab, ListFilter tabFilter, string label)
    {
        if (tab != null)
            tab.Init(label, null, () => ShowFilter(tabFilter), filter == tabFilter);
    }

    private void ShowFilter(ListFilter newFilter)
    {
        if (filter == newFilter)
            return;

        filter = newFilter;
        BuildList();
    }

    private void HandleQuestChanged(QuestData quest, QuestStatus status)
    {
        BuildList(false);
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

    // Every stage reached so far, one row per step, the oldest at the top: done ones ticked and greyed,
    // the stage under way below them; the stages still to come are not shown. A completed quest shows
    // all of them ticked. The count goes on the step it belongs to.
    private void ApplyObjective(QuestData quest, QuestStatus status)
    {
        bool completed = status == QuestStatus.Completed;

        if (objectivesRoot != null)
            objectivesRoot.SetActive(true);

        if (objectiveText == null)
            return;

        string count = QuestManager.TryGetObjectiveProgress(quest, out int current, out int target)
            ? $" ({current}/{target})"
            : string.Empty;

        int used = 0;
        IReadOnlyList<QuestStep> steps = quest.Steps;
        if (steps == null || steps.Count == 0)
        {
            SetObjectiveRow(used++, quest.ObjectiveLabel + count, completed);
        }
        else
        {
            int reached = completed ? quest.StageCount - 1 : quest.CurrentStage;
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] == null || string.IsNullOrWhiteSpace(steps[i].label) || quest.StageOf(i) > reached
                    || !quest.IsStepReached(i))
                    continue;

                SetObjectiveRow(used++, steps[i].label + (quest.StepShowsCount(i) ? count : string.Empty),
                    completed || quest.IsStepDone(i));
            }
        }

        for (int i = Mathf.Max(1, used); i <= objectiveRows.Count; i++)
            objectiveRows[i - 1].row.SetActive(false);
    }

    // The first row is the one in the window; the others are cloned beside it, in order.
    private void SetObjectiveRow(int index, string text, bool done)
    {
        Transform firstRow = objectiveText.transform.parent;
        TMP_Text label = objectiveText;
        Image box = objectiveCheckbox;
        if (index > 0)
        {
            while (objectiveRows.Count < index)
            {
                GameObject row = Instantiate(firstRow.gameObject, firstRow.parent);
                row.name = firstRow.name;
                objectiveRows.Add((row,
                    row.transform.GetChild(objectiveText.transform.GetSiblingIndex()).GetComponent<TMP_Text>(),
                    objectiveCheckbox != null
                        ? row.transform.GetChild(objectiveCheckbox.transform.GetSiblingIndex()).GetComponent<Image>()
                        : null));
            }

            var clone = objectiveRows[index - 1];
            clone.row.SetActive(true);
            clone.row.transform.SetSiblingIndex(firstRow.GetSiblingIndex() + index);
            label = clone.label;
            box = clone.box;
        }

        if (label != null)
        {
            label.text = text;
            label.color = done ? CompletedObjectiveColor : DetailsTextColor;
        }

        if (box != null)
            box.sprite = done ? checkedObjectiveSprite : uncheckedObjectiveSprite;
    }

    private void ApplyRewards(QuestData quest)
    {
        if (rewardsContainer == null || rewardItemTemplate == null)
            return;

        int rewardCount = quest != null ? quest.ShownRewardCount : 0;
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
                rewardItemViews[i].Setup(quest.GetShownReward(i));
        }
    }
}
