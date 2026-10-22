using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Profile")]
    public CharacterProfile profile;

    [Header("Health")]
    public int maxHealth = 30;
    public int startingArmor = 0;

    [Header("Action Points")]
    public int maxActionPoints = 5;
    public int startActionPoints = 4;
    public int actionPointsPerTurn = 2;

    [Header("Deck")]
    public DeckData Deck;
    public int startHandSize = 5;
    public int cardsDrawnPerTurn = 1;

    [Header("Base Cards")]
    [Tooltip("Haru's own fists and legs: not items, never in the inventory. They fill a battle deck that is short of the minimum, one of each in turn (fist, leg, fist, leg, fist), each up to its count.")]
    public List<BaseCardEntry> baseCards = new();
    [Tooltip("A battle deck with fewer cards than this is topped up with base cards: at the start of a battle and whenever a card leaves it for good (broken, used up).")]
    [Min(0)] public int minimumBattleDeckSize = 5;

    [Header("Inventory")]
    public InventoryData Inventory;

    [Header("Currency")]
    public int startingGold = 35;

    [Header("Skills")]
    [Min(0)] public int startingSkillPoints;
    public List<SkillData> skills = new();

    public string DisplayName => profile != null ? profile.DisplayName : name;
    public Sprite PortraitSprite => profile != null ? profile.PortraitSprite : null;
    public Sprite BattleIcon => profile != null ? profile.BattleIcon : null;

    // The base cards in the order they top up a short deck: one of each in turn, each up to its
    // count. 3 fists and 2 legs: fist, leg, fist, leg, fist.
    public List<CardData> GetBaseCardsInFillOrder()
    {
        var order = new List<CardData>();
        for (int round = 0; ; round++)
        {
            bool added = false;
            foreach (BaseCardEntry entry in baseCards)
            {
                if (entry != null && entry.card != null && round < entry.count)
                {
                    order.Add(entry.card);
                    added = true;
                }
            }

            if (!added)
                return order;
        }
    }

    [Serializable]
    public sealed class BaseCardEntry
    {
        public CardData card;
        [Min(0)] public int count = 1;
    }
}
