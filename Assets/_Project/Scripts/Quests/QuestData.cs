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
    [Tooltip("Story Flags: each one counts once it is set, e.g. the four things gathered for Survival Basics (2/4).")]
    [SerializeField, StoryFlag] private List<string> targetFlags = new();
    [Tooltip("What to do, one stage after another, e.g. take the letter to Garr, then tell Eldon. A step With Previous belongs to the stage of the one before it: both are to be done at once, in any order, e.g. two things to find before bringing them back. The quest tracker shows the stage under way, the quest log every stage reached. Empty = just Objective Label.")]
    [SerializeField] private List<QuestStep> steps = new();

    [Header("Reward")]
    [SerializeField] private List<QuestRewardData> rewards = new();

    public string QuestKey => questKey;
    public string Title => string.IsNullOrWhiteSpace(title) ? name : title;
    public string GiverName => giverName;
    public string Description => description;
    public QuestStatus Status => status;
    public QuestObjectiveType ObjectiveType => objectiveType;
    public string ObjectiveLabel => CurrentStep != null
        ? CurrentStep.label
        : string.IsNullOrWhiteSpace(objectiveLabel) ? GetDefaultObjectiveLabel() : objectiveLabel;
    public string TargetEncounterId => targetEncounterId;
    public EnemyData TargetEnemy => targetEnemy;
    public CardData TargetCard => targetCard;
    public int TargetCount => objectiveType == QuestObjectiveType.StoryFlags
        ? Mathf.Max(1, CountTargetFlags(false))
        : Mathf.Max(1, targetCount);
    // Story Flags: how many of them are set.
    public int TargetFlagsSet => CountTargetFlags(true);
    public IReadOnlyList<string> TargetFlags => targetFlags;
    // What to do, one step after another (QuestTracker lists them all). Empty = Objective Label alone.
    public IReadOnlyList<QuestStep> Steps => steps;
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

    // The rewards shown to the player now (QuestRewardData.IsShown); all of them are given all the same.
    public int ShownRewardCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < RewardCount; i++)
            {
                if (GetReward(i).IsShown)
                    count++;
            }

            return count;
        }
    }

    public Sprite ShownRewardIcon => ShownRewardCount > 0 ? GetShownReward(0).Icon : null;

    public QuestRewardData GetShownReward(int index)
    {
        int shown = 0;
        for (int i = 0; i < RewardCount; i++)
        {
            QuestRewardData reward = GetReward(i);
            if (!reward.IsShown)
                continue;

            if (shown == index)
                return reward;

            shown++;
        }

        return null;
    }

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

    // The steps make stages: a step starts a new one unless it goes With Previous. The steps of a stage
    // are to be done at once; the stages one after another.
    public int StageCount
    {
        get
        {
            int count = 0;
            for (int i = 0; steps != null && i < steps.Count; i++)
            {
                if (i == 0 || !IsWithPrevious(i))
                    count++;
            }

            return count;
        }
    }

    public int StageOf(int index)
    {
        int stage = -1;
        for (int i = 0; steps != null && i <= index && i < steps.Count; i++)
        {
            if (i == 0 || !IsWithPrevious(i))
                stage++;
        }

        return Mathf.Max(0, stage);
    }

    // The first stage with a step not done yet; the last one once the others are.
    public int CurrentStage
    {
        get
        {
            int stages = StageCount;
            for (int stage = 0; stage < stages - 1; stage++)
            {
                if (!IsStageDone(stage))
                    return stage;
            }

            return Mathf.Max(0, stages - 1);
        }
    }

    public bool IsStageDone(int stage)
    {
        bool any = false;
        for (int i = 0; steps != null && i < steps.Count; i++)
        {
            if (steps[i] == null || StageOf(i) != stage)
                continue;

            any = true;
            if (!IsStepDone(i))
                return false;
        }

        return any;
    }

    // A step is done once its flag is set, or while its Done While Met conditions are (the herbs in the
    // pockets), or, with neither, once the quest's count is reached (2/4 gathered, 3/3 brought).
    // Without either in the last stage it never is: the quest is completed instead ("Show your father
    // what you found").
    public bool IsStepDone(int index)
    {
        if (steps == null || index < 0 || index >= steps.Count || steps[index] == null)
            return false;

        string flag = steps[index].doneWhen;
        if (!string.IsNullOrWhiteSpace(flag) && StoryFlagManager.HasFlag(flag))
            return true;

        if (HasDoneConditions(index))
            return StoryCondition.AreMet(steps[index].doneWhileMet);

        if (!string.IsNullOrWhiteSpace(flag))
            return false;

        return StageOf(index) < StageCount - 1
            && QuestManager.TryGetObjectiveProgress(this, out int current, out int target) && current >= target;
    }

    // In the quest tracker now: from its stage on, or once its Shown When flag is set; not while a
    // detour of its stage is under way, and a detour itself only until it is done.
    public bool IsStepShown(int index)
    {
        if (!IsStepReached(index))
            return false;

        if (steps[index].detour)
            return !IsStepDone(index);

        int stage = StageOf(index);
        for (int i = 0; i < steps.Count; i++)
        {
            if (i != index && steps[i] != null && steps[i].detour && StageOf(i) == stage && IsStepReached(i) && !IsStepDone(i))
                return false;
        }

        return true;
    }

    // Come up at all (its Shown When flag set, or none): the quest's page lists every such step in order,
    // the step a detour stood in front of above it, e.g. the dust 2/3 over asking Eldon.
    public bool IsStepReached(int index)
    {
        if (steps == null || index < 0 || index >= steps.Count || steps[index] == null)
            return false;

        string flag = steps[index].shownWhen;
        return string.IsNullOrWhiteSpace(flag) || StoryFlagManager.HasFlag(flag);
    }

    // The quest's count (2/4) goes with this step: one without a flag, done once the count is reached,
    // or the only stage's. Not with one in the last of several stages ("Show your father what you found").
    public bool StepShowsCount(int index)
    {
        if (steps == null || steps.Count <= 1)
            return true;

        if (index < 0 || index >= steps.Count || steps[index] == null
            || !string.IsNullOrWhiteSpace(steps[index].doneWhen) || HasDoneConditions(index))
            return false;

        int stages = StageCount;
        return stages == 1 || StageOf(index) < stages - 1;
    }

    private bool HasDoneConditions(int index)
    {
        return steps[index].doneWhileMet != null && steps[index].doneWhileMet.Count > 0;
    }

    private bool IsWithPrevious(int index)
    {
        return steps[index] != null && steps[index].withPrevious;
    }

    // The first step of the stage under way that is not done yet; its first one if all are.
    private QuestStep CurrentStep
    {
        get
        {
            if (steps == null || steps.Count == 0)
                return null;

            int stage = CurrentStage;
            QuestStep first = null;
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] == null || StageOf(i) != stage)
                    continue;

                first ??= steps[i];
                if (!IsStepDone(i))
                    return steps[i];
            }

            return first ?? steps[steps.Count - 1];
        }
    }

    private int CountTargetFlags(bool onlySet)
    {
        int count = 0;
        if (targetFlags == null)
            return count;

        foreach (string flag in targetFlags)
        {
            if (!string.IsNullOrWhiteSpace(flag) && (!onlySet || StoryFlagManager.HasFlag(flag)))
                count++;
        }

        return count;
    }

    private string GetDefaultObjectiveLabel()
    {
        return objectiveType switch
        {
            QuestObjectiveType.DefeatEncounter => "Defeat the marked enemy",
            QuestObjectiveType.DefeatEnemies when targetEnemy != null => $"Defeat {TargetCount}x {targetEnemy.DisplayName}",
            QuestObjectiveType.CollectCards when targetCard != null => $"Bring {TargetCount}x {GetCardName(targetCard)}",
            QuestObjectiveType.StoryFlags => "Do what is asked",
            _ => "Complete the requested task"
        };
    }

    private static string GetCardName(CardData card)
    {
        return string.IsNullOrWhiteSpace(card.CardName) ? card.name : card.CardName;
    }
}

// One step of a quest's objective (QuestData.steps).
[Serializable]
public sealed class QuestStep
{
    [Tooltip("What the quest log says to do, e.g. Take Eldon's letter to Garr's forge.")]
    public string label;
    [Tooltip("Done once this flag is set. Empty = once the quest's count is reached.")]
    [StoryFlag] public string doneWhen;
    [Tooltip("Done also while these are met, before the flag, e.g. Has Card for each herb Garr asked for: all in the pockets, the next step (take them to him) comes up; eat the berries and it is back. Empty = only the flag (or the count).")]
    public List<StoryCondition> doneWhileMet = new();
    [Tooltip("Done at once with the step before it, in any order, e.g. a second thing to find: shown together, and the next stage comes once both are done.")]
    public bool withPrevious;
    [Tooltip("Shown in the log only once this flag is set: a step that comes up along the way, e.g. asking Eldon about the last handful once Haru cannot find it. Empty = shown with its stage.")]
    [StoryFlag] public string shownWhen;
    [Tooltip("A detour in the quest tracker: while it is shown and not done, the other steps of its stage step aside for it there; once it is done it goes, and they come back. E.g. asking Eldon about the last handful in place of looking for the dust. The quest's page lists them all in order.")]
    public bool detour;
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

    // Shown in the quest log, the tracker and the toasts only once the HUD shows what it gives:
    // skill points and gold come in quietly until the story has brought them up (HudFlags).
    public bool IsShown => type switch
    {
        QuestRewardType.SkillPoints => HudFlags.IsOpen(HudFlags.Skills),
        QuestRewardType.Gold => HudFlags.IsOpen(HudFlags.Gold),
        _ => true
    };

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
    CollectCards,
    // Set a few story flags, e.g. the four things gathered for Survival Basics: shows how many (2/4).
    StoryFlags
}
