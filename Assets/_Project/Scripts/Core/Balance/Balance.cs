using System.Collections.Generic;
using UnityEngine;

// Works the Balance Config into numbers the game uses: what a card is worth in gold (from its
// effects, unless it has a gold value of its own), shop prices and how much health enemies get in
// groups or against Haru and his father together.
public static class Balance
{
    private static BalanceConfig config;

    public static BalanceConfig Config
    {
        get
        {
            if (config == null)
                config = Resources.Load<BalanceConfig>(BalanceConfig.ResourcePath);
            if (config == null)
                config = ScriptableObject.CreateInstance<BalanceConfig>();

            return config;
        }
    }

    // A card's worth in gold, from its effects: damage, armor, healing, statuses, each at its
    // gold per point; less for every action point above 1.
    public static int CardValue(CardData card)
    {
        if (card == null)
            return 0;

        float worth = EffectsWorth(card.InstantEffects, card.StatusApplications);
        if (worth <= 0f)
            return Config.valueWithoutEffects;

        worth -= Mathf.Max(0, card.Cost - 1) * Config.goldPerExtraActionPoint;
        return Mathf.Max(Config.minimumCardValue, Mathf.RoundToInt(worth));
    }

    public static float EffectsWorth<TInstant, TStatus>(IReadOnlyList<TInstant> instants, IReadOnlyList<TStatus> statuses)
        where TInstant : class, IInstantEffectSpec
        where TStatus : class, IStatusApplicationSpec
    {
        float worth = 0f;
        if (instants != null)
        {
            foreach (TInstant spec in instants)
                worth += InstantWorth(spec);
        }

        if (statuses != null)
        {
            foreach (TStatus spec in statuses)
                worth += StatusWorth(spec);
        }

        return worth;
    }

    public static float InstantWorth(IInstantEffectSpec spec)
    {
        if (spec?.Effect == null)
            return 0f;

        float perPoint = spec.Effect switch
        {
            DamageEffect damage => Config.goldPerDamage * (1f + damage.ArmorPierce * Config.pierceBonus),
            GainArmorEffect => Config.goldPerArmor,
            HealEffect => Config.goldPerHeal,
            _ => 0f
        };

        return spec.Value * perPoint * Mathf.Clamp01(spec.ApplyChance) * TargetsMultiplier(spec.Targeting);
    }

    public static float StatusWorth(IStatusApplicationSpec spec)
    {
        if (spec?.Status == null)
            return 0f;

        int turns = Mathf.Max(1, spec.Duration);
        int stacks = Mathf.Max(1, spec.Stacks);
        float worth = spec.Status switch
        {
            PeriodicDamageStatus => spec.Value * turns * stacks * Config.goldPerDamageOverTime,
            RegenerationStatus => spec.Value * turns * stacks * Config.goldPerRegeneration,
            StunStatus => Mathf.Max(1, spec.Value) * stacks * Config.goldPerStunnedPoint,
            _ => 0f
        };

        return worth * Mathf.Clamp01(spec.ApplyChance) * TargetsMultiplier(spec.Targeting);
    }

    public static bool HitsEveryone(BattleTargetingProfile targeting)
    {
        return targeting is BattleTargetAllEnemiesProfile
            || targeting is BattleTargetAllAlliesProfile
            || targeting is BattleTargetAllCombatantsProfile
            || targeting is BattleTargetAllOtherCombatantsProfile;
    }

    public static bool CanSell(CardData card)
    {
        if (card == null)
            return false;

        foreach (CardCategory category in card.Categories)
        {
            if (Config.unsellableCategories.Contains(category))
                return false;
        }

        return true;
    }

    public static int BuyPrice(CardData card, float shopMultiplier = 1f)
    {
        return card != null ? RoundGold(card.BaseGoldValue * Config.buyMarkup * shopMultiplier) : 0;
    }

    public static int SellPrice(CardData card, float shopMultiplier = 1f)
    {
        return CanSell(card) ? RoundGold(card.BaseGoldValue * Config.sellShare * shopMultiplier) : 0;
    }

    // Health of an enemy in the battle being fought: groups and the father's help change it.
    public static int EnemyMaxHealth(int baseHealth)
    {
        int groupSize = Mathf.Max(1, PendingBattleEnemy.Enemies?.Count ?? 1);
        bool fatherFights = (PendingBattleEnemy.Allies?.Count ?? 0) > 0;
        return Mathf.Max(1, Mathf.RoundToInt(baseHealth * EnemyHealthMultiplier(groupSize, fatherFights)));
    }

    public static float EnemyHealthMultiplier(int groupSize, bool fatherFights)
    {
        float multiplier = Config.enemyHealthMultiplier;
        if (groupSize == 2)
            multiplier *= Config.healthInGroupOfTwo;
        else if (groupSize >= 3)
            multiplier *= Config.healthInGroupOfThree;

        if (fatherFights)
            multiplier *= Config.healthWhenFatherFights;

        return multiplier;
    }

    private static float TargetsMultiplier(BattleTargetingProfile targeting)
    {
        return HitsEveryone(targeting) ? Config.allTargetsMultiplier : 1f;
    }

    private static int RoundGold(float value)
    {
        return value <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(value));
    }
}
