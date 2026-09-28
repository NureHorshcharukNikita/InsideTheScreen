using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Game/Quests/Quest")]
public sealed class QuestData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string questKey;
    [SerializeField] private string title;
    [SerializeField] private string giverName;

    [TextArea(4, 8)]
    [SerializeField] private string description;

    [Header("State")]
    [SerializeField] private QuestStatus status = QuestStatus.Available;

    [Header("Objective")]
    [SerializeField] private QuestObjectiveType objectiveType = QuestObjectiveType.Manual;
    [SerializeField] private string objectiveLabel;
    [SerializeField] private string targetEncounterId;

    [Header("Reward")]
    [SerializeField] private List<QuestRewardData> rewards = new();

    [Header("Legacy Reward")]
    [SerializeField] private string rewardLabel;
    [SerializeField] private Sprite rewardIcon;
    [SerializeField] private CardData rewardCard;
    [SerializeField] private int rewardCardCount = 1;

    public string QuestKey => questKey;
    public string Title => string.IsNullOrWhiteSpace(title) ? name : title;
    public string GiverName => giverName;
    public string Description => description;
    public QuestStatus Status => status;
    public QuestObjectiveType ObjectiveType => objectiveType;
    public string ObjectiveLabel => string.IsNullOrWhiteSpace(objectiveLabel)
        ? GetDefaultObjectiveLabel()
        : objectiveLabel;
    public string TargetEncounterId => targetEncounterId;
    public int RewardCount
    {
        get
        {
            int count = 0;

            if (rewards != null)
            {
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i] != null && rewards[i].HasContent)
                        count++;
                }
            }

            return count > 0 || !HasLegacyReward ? count : 1;
        }
    }

    public string RewardLabel => RewardCount > 0 ? GetReward(0).Label : string.Empty;
    public Sprite RewardIcon => RewardCount > 0 ? GetReward(0).Icon : null;
    public CardData RewardCard => RewardCount > 0 ? GetReward(0).Card : null;
    public int RewardCardCount => RewardCount > 0 ? GetReward(0).CardCount : 0;

    public QuestRewardData GetReward(int index)
    {
        if (index < 0)
            return null;

        int rewardIndex = 0;
        if (rewards != null)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                QuestRewardData reward = rewards[i];
                if (reward == null || !reward.HasContent)
                    continue;

                if (rewardIndex == index)
                    return reward;

                rewardIndex++;
            }
        }

        if (rewardIndex == 0 && index == 0 && HasLegacyReward)
            return new QuestRewardData(rewardLabel, rewardIcon, rewardCard, rewardCardCount);

        return null;
    }

    private bool HasLegacyReward => !string.IsNullOrWhiteSpace(rewardLabel)
                                    || rewardIcon != null
                                    || rewardCard != null;

    private string GetDefaultObjectiveLabel()
    {
        return objectiveType == QuestObjectiveType.DefeatEncounter
            ? "Defeat the marked enemy"
            : "Complete the requested task";
    }
}

[Serializable]
public sealed class QuestRewardData
{
    [SerializeField] private string label;
    [SerializeField] private Sprite icon;
    [SerializeField] private CardData card;
    [SerializeField, Min(1)] private int cardCount = 1;

    public string Label => label;
    public Sprite Icon => icon;
    public CardData Card => card;
    public int CardCount => Mathf.Max(0, cardCount);
    public bool HasContent => !string.IsNullOrWhiteSpace(label) || icon != null || card != null;

    public QuestRewardData(string label, Sprite icon, CardData card, int cardCount)
    {
        this.label = label;
        this.icon = icon;
        this.card = card;
        this.cardCount = cardCount;
    }
}

public enum QuestObjectiveType
{
    Manual,
    DefeatEncounter
}
