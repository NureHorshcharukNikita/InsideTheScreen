using System;
using System.Collections.Generic;
using UnityEngine;

// The game's balance in one place, with gold as the common unit: what a point of damage, armor or
// healing is worth, how shops price cards, and how tough enemies get in groups or against Haru and
// his father together. Battles pay only in loot (gold and skill points come from quests) and heal
// nothing. Lives in Resources (BalanceConfig) so the
// game finds it; Window > Game > Balance Report shows what the numbers add up to (prices, how
// long fights last). 1 everywhere = the values written in the cards and enemies themselves.
[CreateAssetMenu(fileName = "BalanceConfig", menuName = "Game/Balance Config")]
public sealed class BalanceConfig : ScriptableObject
{
    public const string ResourcePath = "BalanceConfig";

    [Header("Card value: gold per point")]
    [Tooltip("Gold a card is worth per point of damage it deals (1 = a 6-damage card is worth 6 gold).")]
    [Min(0f)] public float goldPerDamage = 1f;
    [Tooltip("Gold per point of armor it gives.")]
    [Min(0f)] public float goldPerArmor = 0.8f;
    [Tooltip("Gold per point of health it heals. Health is scarce between fights, so a little dearer than damage.")]
    [Min(0f)] public float goldPerHeal = 1.2f;
    [Tooltip("Gold per point of damage over time (burning, poison, bleeding): value x turns x stacks.")]
    [Min(0f)] public float goldPerDamageOverTime = 0.8f;
    [Tooltip("Gold per point of regeneration: value x turns x stacks.")]
    [Min(0f)] public float goldPerRegeneration = 1f;
    [Tooltip("Gold per action point a stun takes from the target.")]
    [Min(0f)] public float goldPerStunnedPoint = 6f;
    [Tooltip("Extra worth of damage that ignores armor: 0.5 = the pierced part counts 1.5 times.")]
    [Min(0f)] public float pierceBonus = 0.5f;
    [Tooltip("Worth of an effect on every enemy (or ally) compared to one target.")]
    [Min(0f)] public float allTargetsMultiplier = 1.8f;
    [Tooltip("Taken off a card's worth for every action point above 1 it costs.")]
    [Min(0f)] public float goldPerExtraActionPoint = 2f;
    [Tooltip("The least a card with an effect is worth.")]
    [Min(0)] public int minimumCardValue = 1;
    [Tooltip("Worth of a card with no battle effect and no gold value of its own (junk, unused items).")]
    [Min(0)] public int valueWithoutEffects = 2;

    [Header("Shops")]
    [Tooltip("Shops sell at the card's worth times this (each shop can go up or down from it).")]
    [Min(0f)] public float buyMarkup = 1.5f;
    [Tooltip("Shops buy at the card's worth times this.")]
    [Min(0f)] public float sellShare = 0.5f;
    [Tooltip("Cards in these categories can't be sold, e.g. the father's keepsakes.")]
    public List<CardCategory> unsellableCategories = new() { CardCategory.Keepsake };

    [Header("Enemy toughness")]
    [Tooltip("Every enemy's health is multiplied by this. The easiest way to make fights longer or shorter.")]
    [Min(0.1f)] public float enemyHealthMultiplier = 1f;
    [Tooltip("Health of each enemy in a group of two (each one is weaker, the pair is still harder).")]
    [Min(0.1f)] public float healthInGroupOfTwo = 0.85f;
    [Tooltip("Health of each enemy in a group of three.")]
    [Min(0.1f)] public float healthInGroupOfThree = 0.75f;
    [Tooltip("Enemy health when the father fights alongside Haru: his blows and the hits he takes make the fight easier.")]
    [Min(0.1f)] public float healthWhenFatherFights = 1.2f;

    [Header("Skills")]
    [Tooltip("Health share at or below which Haru counts as low on health (Aggression).")]
    [Range(0f, 1f)] public float lowHealthThreshold = 0.3f;
    [Tooltip("Will to Live: the share of max health a lethal blow leaves Haru with, once a battle.")]
    [Range(0.05f, 1f)] public float willToLiveHealthShare = 0.2f;
    [Tooltip("Counterattack: damage an enemy takes back when it hits Haru.")]
    [Min(0)] public int counterattackDamage = 2;
    [Tooltip("Planning: how much cheaper the first card of each turn is.")]
    [Min(0)] public int firstCardDiscount = 1;
    [Tooltip("The order a typical player buys skills in, one entry per rank (the same skill twice = two ranks). The Balance Report fights with the first ranks of it.")]
    public List<SkillData> referenceBuild = new();

    [Header("Balance Report targets")]
    [Tooltip("How many of Haru's turns a standard fight should last.")]
    [Min(1f)] public float targetTurnsStandard = 4f;
    [Tooltip("How much of Haru's health a standard fight should cost, 0-1.")]
    [Range(0f, 1f)] public float targetHealthLost = 0.25f;
    [Tooltip("Decks the report fights with: what Haru carries early, in the middle and late in Arc 1. Only items: a deck short of the minimum gets his fists and legs, as in battle (Player Data).")]
    public List<ReferenceDeck> referenceDecks = new();

    [Serializable]
    public sealed class ReferenceDeck
    {
        public string name = "Deck";
        public List<CardData> cards = new();
    }
}
