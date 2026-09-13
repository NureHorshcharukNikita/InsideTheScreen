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
    [SerializeField] private string targetEncounterId;

    [Header("Reward")]
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
    public string TargetEncounterId => targetEncounterId;
    public string RewardLabel => rewardLabel;
    public Sprite RewardIcon => rewardIcon;
    public CardData RewardCard => rewardCard;
    public int RewardCardCount => Mathf.Max(0, rewardCardCount);
}

public enum QuestObjectiveType
{
    Manual,
    DefeatEncounter
}
