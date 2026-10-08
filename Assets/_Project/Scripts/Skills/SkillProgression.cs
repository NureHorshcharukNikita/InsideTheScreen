using System;
using System.Collections.Generic;
using UnityEngine;

public static class SkillProgression
{
    private static readonly List<SkillProgressSaveData> Progress = new();
    private static readonly List<SkillData> KnownSkills = new();
    private static int availablePoints;
    private static bool initialized;

    public static int AvailablePoints => availablePoints;
    public static bool IsInitialized => initialized;
    public static IReadOnlyList<SkillData> Skills => KnownSkills;

    public static event Action Changed;

    public static void Initialize(int startingPoints, IReadOnlyList<SkillData> skillCatalog)
    {
        RegisterSkills(skillCatalog);
        if (initialized)
        {
            Changed?.Invoke();
            return;
        }

        initialized = true;
        availablePoints = Mathf.Max(0, startingPoints);
        Changed?.Invoke();
    }

    public static void RegisterSkills(IReadOnlyList<SkillData> skills)
    {
        if (skills == null)
            return;

        for (int i = 0; i < skills.Count; i++)
            RegisterSkill(skills[i]);
    }

    public static void RegisterSkill(SkillData skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
            return;

        for (int i = 0; i < KnownSkills.Count; i++)
        {
            if (KnownSkills[i] != null && KnownSkills[i].SkillId == skill.SkillId)
                return;
        }

        KnownSkills.Add(skill);
    }

    public static int GetRank(SkillData skill)
    {
        return skill == null ? 0 : GetRank(skill.SkillId);
    }

    public static int GetRank(string skillId)
    {
        SkillProgressSaveData progress = GetProgress(skillId);
        return progress != null ? Mathf.Max(0, progress.rank) : 0;
    }

    public static SkillPurchaseStatus GetPurchaseStatus(SkillData skill)
    {
        if (!initialized || skill == null || string.IsNullOrWhiteSpace(skill.SkillId) || skill.MaxRank <= 0)
            return SkillPurchaseStatus.InvalidSkill;

        RegisterSkill(skill);
        int currentRank = GetRank(skill);
        if (currentRank >= skill.MaxRank)
            return SkillPurchaseStatus.MaxRank;

        if (!ArePrerequisitesMet(skill))
            return SkillPurchaseStatus.MissingPrerequisites;

        if (!AreProgressionRequirementsMet(skill))
            return SkillPurchaseStatus.LockedByProgress;

        SkillRankData nextRank = skill.GetRankData(currentRank + 1);
        if (nextRank == null)
            return SkillPurchaseStatus.InvalidSkill;

        return availablePoints >= nextRank.Cost
            ? SkillPurchaseStatus.Available
            : SkillPurchaseStatus.NotEnoughPoints;
    }

    public static bool IsUnlocked(SkillData skill)
    {
        return initialized
            && skill != null
            && ArePrerequisitesMet(skill)
            && AreProgressionRequirementsMet(skill);
    }

    public static int GetNextRankCost(SkillData skill)
    {
        if (skill == null)
            return 0;

        SkillRankData nextRank = skill.GetRankData(GetRank(skill) + 1);
        return nextRank?.Cost ?? 0;
    }

    public static bool TryPurchase(SkillData skill)
    {
        if (GetPurchaseStatus(skill) != SkillPurchaseStatus.Available)
            return false;

        int nextRank = GetRank(skill) + 1;
        SkillRankData rankData = skill.GetRankData(nextRank);
        availablePoints -= rankData.Cost;

        SkillProgressSaveData progress = GetProgress(skill.SkillId);
        if (progress == null)
        {
            progress = new SkillProgressSaveData { skillId = skill.SkillId };
            Progress.Add(progress);
        }

        progress.rank = nextRank;
        Changed?.Invoke();
        ExplorationPlayerSession.SaveSkillProgression();
        return true;
    }

    public static void AddPoints(int amount)
    {
        if (amount <= 0)
            return;

        EnsureInitialized();
        SetAvailablePoints(availablePoints + amount);
    }

    public static void SetAvailablePoints(int amount)
    {
        EnsureInitialized();
        int clampedAmount = Mathf.Max(0, amount);
        if (availablePoints == clampedAmount)
            return;

        availablePoints = clampedAmount;
        Changed?.Invoke();
        ExplorationPlayerSession.SaveSkillProgression();
    }

    public static int ModifyInt(SkillStat stat, int baseValue)
    {
        return Mathf.RoundToInt(ModifyFloat(stat, baseValue));
    }

    public static float ModifyFloat(SkillStat stat, float baseValue)
    {
        float flat = GetModifier(stat, SkillModifierType.Flat);
        float percent = GetModifier(stat, SkillModifierType.Percent);
        return (baseValue + flat) * Mathf.Max(0f, 1f + percent / 100f);
    }

    public static bool HasPassive(SkillPassive passive)
    {
        if (passive == SkillPassive.None)
            return false;

        foreach (SkillEffectData effect in GetActiveEffects())
        {
            if (effect.Kind == SkillEffectKind.Passive && effect.Passive == passive)
                return true;
        }

        return false;
    }

    public static SkillProgressSaveData[] ToArray()
    {
        SkillProgressSaveData[] result = new SkillProgressSaveData[Progress.Count];
        for (int i = 0; i < Progress.Count; i++)
        {
            result[i] = new SkillProgressSaveData
            {
                skillId = Progress[i].skillId,
                rank = Progress[i].rank
            };
        }

        return result;
    }

    public static void Restore(bool hasProgression, int savedPoints, SkillProgressSaveData[] savedProgress)
    {
        Progress.Clear();
        initialized = hasProgression;
        availablePoints = hasProgression ? Mathf.Max(0, savedPoints) : 0;

        if (savedProgress != null)
        {
            for (int i = 0; i < savedProgress.Length; i++)
            {
                SkillProgressSaveData saved = savedProgress[i];
                if (saved == null || string.IsNullOrWhiteSpace(saved.skillId) || saved.rank <= 0)
                    continue;

                Progress.Add(new SkillProgressSaveData
                {
                    skillId = saved.skillId,
                    rank = saved.rank
                });
            }
        }

        Changed?.Invoke();
    }

    public static void Clear()
    {
        initialized = false;
        availablePoints = 0;
        Progress.Clear();
        KnownSkills.Clear();
        Changed?.Invoke();
    }

    private static bool ArePrerequisitesMet(SkillData skill)
    {
        IReadOnlyList<SkillPrerequisite> prerequisites = skill.Prerequisites;
        if (prerequisites == null)
            return true;

        for (int i = 0; i < prerequisites.Count; i++)
        {
            SkillPrerequisite prerequisite = prerequisites[i];
            if (prerequisite == null || prerequisite.Skill == null)
                continue;

            if (GetRank(prerequisite.Skill) < prerequisite.RequiredRank)
                return false;
        }

        return true;
    }

    private static bool AreProgressionRequirementsMet(SkillData skill)
    {
        IReadOnlyList<SkillUnlockRequirement> requirements = skill.ProgressionRequirements;
        if (requirements == null)
            return true;

        for (int i = 0; i < requirements.Count; i++)
        {
            SkillUnlockRequirement requirement = requirements[i];
            if (requirement == null)
                continue;

            bool met = requirement.Type switch
            {
                SkillUnlockRequirementType.StoryFlag => StoryFlagManager.HasFlag(requirement.StoryFlag),
                SkillUnlockRequirementType.QuestStatus => requirement.Quest != null
                    && QuestManager.GetStatus(requirement.Quest) == requirement.RequiredQuestStatus,
                SkillUnlockRequirementType.DefeatedEncounter => DefeatedEncounters.IsDefeated(requirement.EncounterId),
                _ => false
            };

            if (!met)
                return false;
        }

        return true;
    }

    private static float GetModifier(SkillStat stat, SkillModifierType modifierType)
    {
        float total = 0f;
        foreach (SkillEffectData effect in GetActiveEffects())
        {
            if (effect.Kind == SkillEffectKind.StatModifier
                && effect.Stat == stat
                && effect.ModifierType == modifierType)
                total += effect.Value;
        }

        return total;
    }

    private static IEnumerable<SkillEffectData> GetActiveEffects()
    {
        for (int skillIndex = 0; skillIndex < KnownSkills.Count; skillIndex++)
        {
            SkillData skill = KnownSkills[skillIndex];
            if (skill == null)
                continue;

            int rank = Mathf.Min(GetRank(skill), skill.MaxRank);
            for (int rankIndex = 1; rankIndex <= rank; rankIndex++)
            {
                IReadOnlyList<SkillEffectData> effects = skill.GetRankData(rankIndex)?.Effects;
                if (effects == null)
                    continue;

                for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
                {
                    SkillEffectData effect = effects[effectIndex];
                    if (effect != null)
                        yield return effect;
                }
            }
        }
    }

    private static SkillProgressSaveData GetProgress(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return null;

        for (int i = 0; i < Progress.Count; i++)
        {
            if (Progress[i] != null && Progress[i].skillId == skillId)
                return Progress[i];
        }

        return null;
    }

    private static void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        availablePoints = 0;
    }
}

public enum SkillPurchaseStatus
{
    Available,
    InvalidSkill,
    MaxRank,
    MissingPrerequisites,
    LockedByProgress,
    NotEnoughPoints
}
