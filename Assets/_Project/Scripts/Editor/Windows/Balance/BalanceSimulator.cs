using System.Collections.Generic;
using UnityEngine;

// A quick estimate of how a fight goes, from averages instead of dice: Haru plays his deck's
// average card as often as his action points and draws allow, the father (if he fights) uses his
// abilities as often as their weights and cooldowns say, enemies the same. Everyone focuses one
// enemy at a time; enemies split their blows between Haru and the father. Good enough to see
// whether a fight is a short skirmish or a slog, and how much health it costs.
public static class BalanceSimulator
{
    private const int MaxTurns = 40;

    public sealed class DeckProfile
    {
        public float averageCost = 1f;
        public float damageToOne;      // per card played: damage (and damage over time) to one enemy
        public float damageToAll;      // per card played: to every enemy
        public float defense;          // per card played: armor + healing + regeneration
        public float healingShare;     // the part of defense that heals (can't go above max health)
        public float hitCardShare;     // share of cards that land a blow (Strength counts on them)
        public float healCardShare;    // share of cards that heal (Equilibrium counts on them)
        public int cardCount;
    }

    // What Haru's bought skills add, in the same flat amounts the game uses.
    public sealed class SkillBonuses
    {
        public int pointsSpent;
        public readonly Dictionary<SkillStat, float> stats = new();
        public readonly HashSet<SkillPassive> passives = new();

        public float Get(SkillStat stat) => stats.TryGetValue(stat, out float value) ? value : 0f;

        // The first ranks of a build order that the points pay for.
        public static SkillBonuses FromBuild(IReadOnlyList<SkillData> buildOrder, int points)
        {
            var bonuses = new SkillBonuses();
            var ranks = new Dictionary<SkillData, int>();
            if (buildOrder == null)
                return bonuses;

            foreach (SkillData skill in buildOrder)
            {
                if (skill == null)
                    continue;

                int rank = ranks.TryGetValue(skill, out int current) ? current + 1 : 1;
                SkillRankData data = skill.GetRankData(rank);
                if (data == null)
                    continue;
                if (bonuses.pointsSpent + data.Cost > points)
                    break;

                bonuses.pointsSpent += data.Cost;
                ranks[skill] = rank;
                foreach (SkillEffectData effect in data.Effects)
                {
                    if (effect == null)
                        continue;
                    if (effect.Kind == SkillEffectKind.Passive)
                        bonuses.passives.Add(effect.Passive);
                    else if (effect.ModifierType == SkillModifierType.Flat)
                        bonuses.stats[effect.Stat] = bonuses.Get(effect.Stat) + effect.Value;
                }
            }

            return bonuses;
        }
    }

    public sealed class FighterProfile
    {
        public string name;
        public int health;
        public int armor;
        public float damageToOne;      // per turn
        public float damageToAll;      // per turn
        public float selfDefense;      // per turn: its own armor and healing
        public float supportHealing;   // per turn: healing or armor for its side
        public int actionsPerTurn = 1; // blows a turn, for skills that work per hit
    }

    public struct Result
    {
        public bool won;
        public int turns;
        public float haruHealthLost;   // 0-1
        public float fatherHealthLost; // 0-1, or -1 without the father
    }

    public static DeckProfile ProfileDeck(IReadOnlyList<CardData> cards)
    {
        var profile = new DeckProfile();
        if (cards == null)
            return profile;

        float cost = 0f;
        float healing = 0f;
        int hitCards = 0;
        int healCards = 0;
        foreach (CardData card in cards)
        {
            if (card == null)
                continue;

            profile.cardCount++;
            cost += Mathf.Max(0, card.Cost);
            bool hits = false;
            bool heals = false;
            foreach (CardInstantEffectEntry spec in card.InstantEffects)
            {
                hits |= spec?.Effect is DamageEffect;
                heals |= spec?.Effect is HealEffect;
            }

            hitCards += hits ? 1 : 0;
            healCards += heals ? 1 : 0;
            Accumulate(card.InstantEffects, card.StatusApplications, 1f,
                ref profile.damageToOne, ref profile.damageToAll, ref profile.defense, ref profile.defense);
            healing += Healing(card.InstantEffects, card.StatusApplications);
        }

        if (profile.cardCount == 0)
            return profile;

        profile.healingShare = profile.defense > 0f ? Mathf.Clamp01(healing / profile.defense) : 0f;
        profile.hitCardShare = hitCards / (float)profile.cardCount;
        profile.healCardShare = healCards / (float)profile.cardCount;
        profile.averageCost = Mathf.Max(0.5f, cost / profile.cardCount);
        profile.damageToOne /= profile.cardCount;
        profile.damageToAll /= profile.cardCount;
        profile.defense /= profile.cardCount;
        return profile;
    }

    // What an enemy (or the father) does on an average turn: each ability weighed by its selection
    // weight, less often with a cooldown, half as often when it only works under a condition.
    public static FighterProfile ProfileFighter(EnemyData data, float healthMultiplier = 1f)
    {
        var profile = new FighterProfile
        {
            name = data != null ? data.DisplayName : "?",
            health = data != null ? Mathf.Max(1, Mathf.RoundToInt(data.maxHealth * healthMultiplier)) : 1,
            armor = data != null ? data.startingArmor : 0
        };
        if (data == null || data.abilities == null)
            return profile;

        float totalWeight = 0f;
        var weights = new List<float>();
        foreach (EnemyAbilityData ability in data.abilities)
        {
            float weight = 0f;
            if (ability != null)
            {
                weight = ability.selectionWeight / (1f + ability.cooldownTurns);
                if (ability.conditions != null && ability.conditions.Count > 0)
                    weight *= 0.5f;
            }

            weights.Add(weight);
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return profile;

        int actions = Mathf.Max(1, data.actionPointsPerTurn);
        profile.actionsPerTurn = actions;
        for (int i = 0; i < data.abilities.Count; i++)
        {
            EnemyAbilityData ability = data.abilities[i];
            if (ability == null || weights[i] <= 0f)
                continue;

            float share = weights[i] / totalWeight * actions / Mathf.Max(1, ability.actionPointCost);
            Accumulate(ability.instantEffects, ability.statusApplications, share,
                ref profile.damageToOne, ref profile.damageToAll, ref profile.selfDefense, ref profile.supportHealing);
        }

        return profile;
    }

    public static Result Simulate(PlayerData player, DeckProfile deck, FighterProfile father, IReadOnlyList<FighterProfile> enemies,
        SkillBonuses skills = null)
    {
        skills ??= new SkillBonuses();
        BalanceConfig config = Balance.Config;
        float haruMax = (player != null ? player.maxHealth : 30) + skills.Get(SkillStat.MaxHealth);
        float haru = haruMax;
        float haruShield = (player != null ? player.startingArmor : 0) + skills.Get(SkillStat.StartingArmor);
        bool willToLive = skills.passives.Contains(SkillPassive.WillToLive);
        float hitBonus = skills.Get(SkillStat.PhysicalDamage);
        float lowHealthBonus = skills.Get(SkillStat.LowHealthDamage);
        float hitTaken = skills.Get(SkillStat.PhysicalDamageTaken);
        float counter = skills.passives.Contains(SkillPassive.Counterattack) ? config.counterattackDamage : 0f;
        float fatherMax = father != null ? father.health : 0;
        float fatherHp = fatherMax;
        float fatherShield = father != null ? father.armor : 0;

        var hp = new float[enemies.Count];
        var shield = new float[enemies.Count];
        for (int i = 0; i < enemies.Count; i++)
        {
            hp[i] = enemies[i].health;
            shield[i] = enemies[i].armor;
        }

        float maxAp = (player != null ? player.maxActionPoints : 5) + skills.Get(SkillStat.MaxActionPoints);
        float startAp = (player != null ? player.startActionPoints : 4) + skills.Get(SkillStat.StartingActionPoints);
        float apPerTurn = (player != null ? player.actionPointsPerTurn : 2) + skills.Get(SkillStat.ActionPointsPerTurn);
        float handSize = (player != null ? player.startHandSize : 5) + skills.Get(SkillStat.StartingHandSize);
        float drawn = (player != null ? player.cardsDrawnPerTurn : 1) + skills.Get(SkillStat.CardsDrawnPerTurn);
        float discount = skills.passives.Contains(SkillPassive.FirstCardDiscount) ? config.firstCardDiscount : 0f;

        float ap = Mathf.Min(maxAp, startAp);
        float hand = handSize;
        for (int turn = 1; turn <= MaxTurns; turn++)
        {
            // Haru: as many cards as his points and his hand allow.
            if (turn > 1)
            {
                ap = Mathf.Min(maxAp, ap + apPerTurn);
                hand += drawn;
                haru = Mathf.Min(haruMax, haru + skills.Get(SkillStat.HealthPerTurn));
            }

            float plays = Mathf.Min(hand, (ap + Mathf.Min(discount, deck.averageCost)) / deck.averageCost);
            hand -= plays;
            ap = Mathf.Max(0f, ap + Mathf.Min(discount, deck.averageCost) - plays * deck.averageCost);

            bool lowOnHealth = haru <= haruMax * config.lowHealthThreshold;
            float perHit = hitBonus + (lowOnHealth ? lowHealthBonus : 0f);
            float focusDamage = plays * (deck.damageToOne + deck.hitCardShare * perHit)
                + (father != null && fatherHp > 0f ? father.damageToOne : 0f);
            float allDamage = plays * deck.damageToAll + (father != null && fatherHp > 0f ? father.damageToAll : 0f);
            float defense = plays * deck.defense;
            haruShield += defense * (1f - deck.healingShare) + plays * skills.Get(SkillStat.ArmorEveryThirdCard) / 3f;
            haru = Mathf.Min(haruMax, haru + defense * deck.healingShare + plays * deck.healCardShare * skills.Get(SkillStat.HealingDone));
            if (father != null && fatherHp > 0f)
            {
                haru = Mathf.Min(haruMax, haru + father.supportHealing * 0.5f);
                fatherShield += father.selfDefense + father.supportHealing * 0.5f;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (hp[i] > 0f)
                    Hit(ref hp[i], ref shield[i], allDamage);
            }

            for (int i = 0; i < enemies.Count && focusDamage > 0f; i++)
            {
                if (hp[i] <= 0f)
                    continue;

                float before = hp[i] + shield[i];
                Hit(ref hp[i], ref shield[i], focusDamage);
                focusDamage -= before - Mathf.Max(0f, hp[i]) - shield[i];
            }

            if (AllDead(hp))
                return Finish(true, turn, haru, haruMax, fatherHp, fatherMax, father);

            // Enemies: shield themselves, then hit Haru and the father.
            for (int i = 0; i < enemies.Count; i++)
            {
                if (hp[i] <= 0f)
                    continue;

                shield[i] += enemies[i].selfDefense + enemies[i].supportHealing;
                bool fatherStanding = father != null && fatherHp > 0f;
                float share = fatherStanding ? 0.5f : 1f;
                float hitsOnHaru = enemies[i].actionsPerTurn * share;
                float toHaru = enemies[i].damageToOne * share + enemies[i].damageToAll;
                toHaru = Mathf.Max(0f, toHaru + hitTaken * hitsOnHaru);
                Hit(ref haru, ref haruShield, toHaru);
                if (counter > 0f)
                    Hit(ref hp[i], ref shield[i], counter * hitsOnHaru);
                if (fatherStanding)
                    Hit(ref fatherHp, ref fatherShield, enemies[i].damageToOne * 0.5f + enemies[i].damageToAll);
            }

            if (haru <= 0f && willToLive)
            {
                willToLive = false;
                haru = Mathf.Max(1f, haruMax * config.willToLiveHealthShare);
            }

            if (haru <= 0f)
                return Finish(false, turn, 0f, haruMax, fatherHp, fatherMax, father);

            if (AllDead(hp))
                return Finish(true, turn, haru, haruMax, fatherHp, fatherMax, father);
        }

        return Finish(false, MaxTurns, haru, haruMax, fatherHp, fatherMax, father);
    }

    private static void Accumulate<TInstant, TStatus>(
        IReadOnlyList<TInstant> instants, IReadOnlyList<TStatus> statuses, float share,
        ref float damageToOne, ref float damageToAll, ref float selfDefense, ref float support)
        where TInstant : class, IInstantEffectSpec
        where TStatus : class, IStatusApplicationSpec
    {
        if (instants != null)
        {
            foreach (TInstant spec in instants)
            {
                if (spec?.Effect == null)
                    continue;

                float amount = spec.Value * Mathf.Clamp01(spec.ApplyChance) * share;
                bool everyone = Balance.HitsEveryone(spec.Targeting);
                switch (spec.Effect)
                {
                    case DamageEffect:
                        if (everyone)
                            damageToAll += amount;
                        else
                            damageToOne += amount;
                        break;
                    case GainArmorEffect:
                    case HealEffect:
                        if (spec.Targeting is BattleTargetSelfProfile)
                            selfDefense += amount;
                        else
                            support += amount;
                        break;
                }
            }
        }

        if (statuses == null)
            return;

        foreach (TStatus spec in statuses)
        {
            if (spec?.Status == null)
                continue;

            float amount = spec.Value * Mathf.Max(1, spec.Duration) * Mathf.Max(1, spec.Stacks) * Mathf.Clamp01(spec.ApplyChance) * share;
            bool everyone = Balance.HitsEveryone(spec.Targeting);
            switch (spec.Status)
            {
                case PeriodicDamageStatus:
                    if (everyone)
                        damageToAll += amount;
                    else
                        damageToOne += amount;
                    break;
                case RegenerationStatus:
                    if (spec.Targeting is BattleTargetSelfProfile)
                        selfDefense += amount;
                    else
                        support += amount;
                    break;
            }
        }
    }

    private static float Healing<TInstant, TStatus>(IReadOnlyList<TInstant> instants, IReadOnlyList<TStatus> statuses)
        where TInstant : class, IInstantEffectSpec
        where TStatus : class, IStatusApplicationSpec
    {
        float healing = 0f;
        if (instants != null)
        {
            foreach (TInstant spec in instants)
            {
                if (spec?.Effect is HealEffect)
                    healing += spec.Value * Mathf.Clamp01(spec.ApplyChance);
            }
        }

        if (statuses != null)
        {
            foreach (TStatus spec in statuses)
            {
                if (spec?.Status is RegenerationStatus)
                    healing += spec.Value * Mathf.Max(1, spec.Duration) * Mathf.Max(1, spec.Stacks) * Mathf.Clamp01(spec.ApplyChance);
            }
        }

        return healing;
    }

    private static void Hit(ref float health, ref float shield, float damage)
    {
        if (damage <= 0f)
            return;

        float absorbed = Mathf.Min(shield, damage);
        shield -= absorbed;
        health -= damage - absorbed;
    }

    private static bool AllDead(float[] hp)
    {
        foreach (float value in hp)
        {
            if (value > 0f)
                return false;
        }

        return true;
    }

    private static Result Finish(bool won, int turns, float haru, float haruMax, float fatherHp, float fatherMax, FighterProfile father)
    {
        return new Result
        {
            won = won,
            turns = turns,
            haruHealthLost = Mathf.Clamp01(1f - Mathf.Max(0f, haru) / Mathf.Max(1f, haruMax)),
            fatherHealthLost = father != null ? Mathf.Clamp01(1f - Mathf.Max(0f, fatherHp) / Mathf.Max(1f, fatherMax)) : -1f
        };
    }
}
