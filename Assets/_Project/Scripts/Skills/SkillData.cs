using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Skills/Skill")]
public sealed class SkillData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string skillId;
    [SerializeField] private string displayName;
    [SerializeField] private SkillCategory category;
    [SerializeField, Range(1, 5)] private int treePage = 1;
    [SerializeField, Min(1)] private int treeOrder = 1;
    [SerializeField] private Sprite icon;

    [TextArea(3, 7)]
    [SerializeField] private string description;

    [Header("Ranks")]
    [SerializeField] private List<SkillRankData> ranks = new();

    [Header("Unlock Requirements")]
    [SerializeField] private List<SkillPrerequisite> prerequisites = new();
    [SerializeField] private List<SkillUnlockRequirement> progressionRequirements = new();

    public string SkillId => skillId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public SkillCategory Category => category;
    public int TreePage => Mathf.Clamp(treePage, 1, 5);
    public int TreeOrder => Mathf.Max(1, treeOrder);
    public Sprite Icon => icon;
    public string Description => description;
    public int MaxRank => ranks?.Count ?? 0;
    public IReadOnlyList<SkillPrerequisite> Prerequisites => prerequisites;
    public IReadOnlyList<SkillUnlockRequirement> ProgressionRequirements => progressionRequirements;

    public SkillRankData GetRankData(int rank)
    {
        int index = rank - 1;
        return ranks != null && index >= 0 && index < ranks.Count ? ranks[index] : null;
    }

    private void OnValidate()
    {
        skillId = skillId?.Trim();
        treePage = Mathf.Clamp(treePage, 1, 5);
        treeOrder = Mathf.Max(1, treeOrder);
    }
}

[Serializable]
public sealed class SkillRankData
{
    [SerializeField, Min(0)] private int cost = 1;

    [TextArea(2, 5)]
    [SerializeField] private string description;

    [SerializeField] private List<SkillEffectData> effects = new();

    public int Cost => Mathf.Max(0, cost);
    public string Description => description;
    public IReadOnlyList<SkillEffectData> Effects => effects;
}

[Serializable]
public sealed class SkillEffectData
{
    [SerializeField] private SkillEffectKind kind;
    [SerializeField] private SkillStat stat;
    [SerializeField] private SkillModifierType modifierType;
    [SerializeField] private float value;
    [SerializeField] private SkillPassive passive;

    public SkillEffectKind Kind => kind;
    public SkillStat Stat => stat;
    public SkillModifierType ModifierType => modifierType;
    public float Value => value;
    public SkillPassive Passive => passive;
}

[Serializable]
public sealed class SkillPrerequisite
{
    [SerializeField] private SkillData skill;
    [SerializeField, Min(1)] private int requiredRank = 1;

    public SkillData Skill => skill;
    public int RequiredRank => Mathf.Max(1, requiredRank);
}

[Serializable]
public sealed class SkillUnlockRequirement
{
    [SerializeField] private SkillUnlockRequirementType type;
    [SerializeField, StoryFlag] private string storyFlag;
    [SerializeField] private QuestData quest;
    [SerializeField] private QuestStatus requiredQuestStatus = QuestStatus.Completed;
    [SerializeField] private string encounterId;

    public SkillUnlockRequirementType Type => type;
    public string StoryFlag => storyFlag;
    public QuestData Quest => quest;
    public QuestStatus RequiredQuestStatus => requiredQuestStatus;
    public string EncounterId => encounterId;
}

public enum SkillCategory
{
    Physical,
    Knowledge,
    Mental,
    PhysicalKnowledge,
    KnowledgeMental
}

public enum SkillEffectKind
{
    StatModifier,
    Passive
}

public enum SkillModifierType
{
    Flat,
    Percent
}

public enum SkillStat
{
    MaxHealth,
    StartingArmor,
    PhysicalDamage,
    PhysicalDamageTaken,
    LowHealthDamage,
    MaxActionPoints,
    CardActionPointCost,
    MentalDamageTaken,
    StabilityLoss,
    DebuffChance,
    BuffDuration,
    CardRetentionChance,
    EmotionRepeatChance,
    BattleAnalysisDamage
}

public enum SkillPassive
{
    None,
    WillToLive,
    FirstCardDiscount,
    Counterattack,
    TacticalThinking,
    Superfocus
}

public enum SkillUnlockRequirementType
{
    StoryFlag,
    QuestStatus,
    DefeatedEncounter
}
