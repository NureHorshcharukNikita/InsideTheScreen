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

// What a skill rank changes, all in flat amounts (Flat modifier): small battle numbers make
// percentages round away to nothing. Saved as numbers, so the order must stay.
public enum SkillStat
{
    MaxHealth,             // + max health
    StartingArmor,         // armor at the start of every battle
    PhysicalDamage,        // + damage on every hit Haru deals (not on burning, poison, bleeding)
    PhysicalDamageTaken,   // + damage on every hit Haru takes: negative = less, never below 0
    LowHealthDamage,       // + damage on every hit while Haru is low on health (Balance Config)
    MaxActionPoints,       // + action points Haru can hold
    ActionPointsPerTurn,   // + action points every turn
    HealingDone,           // + health on every heal Haru gives
    HealthPerTurn,         // health back at the start of each of Haru's turns
    DamageOverTimeDone,    // + damage on every tick of the burning, poison, bleeding Haru causes
    BuffDuration,          // + turns on statuses Haru puts on his own side (regeneration...)
    StartingHandSize,      // + cards in the opening hand
    StartingActionPoints,  // + action points at the start of a battle
    ArmorEveryThirdCard,   // armor after every third card Haru plays in a battle
    CardsDrawnPerTurn      // + cards drawn every turn
}

public enum SkillPassive
{
    None,
    WillToLive,            // once a battle, a lethal blow leaves Haru on a share of his health
    FirstCardDiscount,     // the first card each turn costs less (Balance Config)
    Counterattack,         // an enemy that hits Haru takes damage back (Balance Config)
    TacticalThinking,      // not used: the skill draws an extra card instead (CardsDrawnPerTurn)
    Superfocus             // not used: the skill gives starting action points instead
}

public enum SkillUnlockRequirementType
{
    StoryFlag,
    QuestStatus,
    DefeatedEncounter
}
