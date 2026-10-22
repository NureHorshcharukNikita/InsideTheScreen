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
    [Tooltip("Defeat Enemies: the kind of enemy that counts (also others with the same profile).")]
    [SerializeField] private EnemyData targetEnemy;
    [Tooltip("Collect Cards: the card to bring.")]
    [SerializeField] private CardData targetCard;
    [Tooltip("Defeat Enemies: how many. Collect Cards: how many to bring.")]
    [SerializeField, Min(1)] private int targetCount = 1;

    [Header("Reward")]
    [SerializeField] private List<QuestRewardData> rewards = new();

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
    public EnemyData TargetEnemy => targetEnemy;
    public CardData TargetCard => targetCard;
    public int TargetCount => Mathf.Max(1, targetCount);
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

            return count;
        }
    }

    public Sprite RewardIcon => RewardCount > 0 ? GetReward(0).Icon : null;

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

        return null;
    }

    // Known as soon as anything that uses it is loaded (an NPC's dialogue, a story event), so a
    // battle can count for it also after a save is loaded.
    private void OnEnable()
    {
        QuestManager.RegisterQuest(this);
    }

    private string GetDefaultObjectiveLabel()
    {
        return objectiveType switch
        {
            QuestObjectiveType.DefeatEncounter => "Defeat the marked enemy",
            QuestObjectiveType.DefeatEnemies when targetEnemy != null => $"Defeat {TargetCount}x {targetEnemy.DisplayName}",
            QuestObjectiveType.CollectCards when targetCard != null => $"Bring {TargetCount}x {GetCardName(targetCard)}",
            _ => "Complete the requested task"
        };
    }

    private static string GetCardName(CardData card)
    {
        return string.IsNullOrWhiteSpace(card.CardName) ? card.name : card.CardName;
    }
}

[Serializable]
public sealed class QuestRewardData
{
    // Drawn by QuestRewardDataDrawer: only the fields of the chosen type are shown.
    [SerializeField] private QuestRewardType type = QuestRewardType.Card;
    [SerializeField] private CardData card;
    [SerializeField, Min(1)] private int cardCount = 1;
    [SerializeField, Min(1)] private int skillPoints = 1;
    [SerializeField, Min(1)] private int gold = 10;

    // Hidden: set by the drawer to the light bulb for skill point rewards.
    // Card rewards leave it empty and show the card's own icon.
    [SerializeField] private Sprite icon;

    public QuestRewardType Type => type;
    public string Label => GetLabel();
    public Sprite Icon => icon != null ? icon : Card != null ? Card.Icon : null;
    public CardData Card => type == QuestRewardType.Card ? card : null;
    public int CardCount => type == QuestRewardType.Card ? Mathf.Max(0, cardCount) : 0;
    public int SkillPoints => type == QuestRewardType.SkillPoints ? Mathf.Max(0, skillPoints) : 0;
    public int Gold => type == QuestRewardType.Gold ? Mathf.Max(0, gold) : 0;
    public bool HasContent => Card != null || SkillPoints > 0 || Gold > 0;

    // E.g. "Meat x1", "+1 Skill Point" or "+20 Gold".
    private string GetLabel()
    {
        if (SkillPoints > 0)
            return $"+{SkillPoints} Skill Point{(SkillPoints > 1 ? "s" : string.Empty)}";

        if (Gold > 0)
            return $"+{Gold} Gold";

        if (Card == null || CardCount <= 0)
            return string.Empty;

        string cardName = string.IsNullOrWhiteSpace(Card.CardName) ? Card.name : Card.CardName;
        return $"{cardName} x{CardCount}";
    }
}

public enum QuestRewardType
{
    Card,
    SkillPoints,
    Gold
}

public enum QuestObjectiveType
{
    Manual,
    DefeatEncounter,
    // Defeat a number of enemies of one kind, anywhere: completes itself at the count.
    DefeatEnemies,
    // Bring a number of one card: done when the inventory has them; completing the quest takes them.
    CollectCards
}
