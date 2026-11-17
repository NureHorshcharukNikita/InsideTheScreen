using System;
using System.Collections.Generic;
using UnityEngine;

public enum StoryConditionType
{
    StoryFlag,
    QuestStatus,
    QuestObjectiveDone,
    // So many of a card in the pockets now, e.g. the herbs Garr asked for.
    HasCard,
    // So many of a card in the deck now; no card picked = any card at all, e.g. Dad's answer to what
    // Haru took into his first fight (Invert: the deck is empty).
    InDeck,
    // Hideshi is with Haru right now: walking with him, or standing close by where Haru is, e.g. his
    // lines in a talk with Sorrel and Fenna (Invert: Haru came alone).
    CompanionWithPlayer
}

// One requirement of an NPC dialogue entry or a story event: a story flag is set,
// or a quest is in a given status, or a Defeat Enemies / Collect Cards quest is active and has
// reached its count (the player can hand it in). Invert turns it around (the flag is not set, the
// quest is not in that status). A list of conditions is met when all of them are; an empty list always is.
[Serializable]
public sealed class StoryCondition
{
    public StoryConditionType type;
    [StoryFlag] public string storyFlag;
    public QuestData quest;
    public QuestStatus questStatus = QuestStatus.Active;
    [Tooltip("Met when the opposite is true: the flag is not set, the quest is not in this status.")]
    public bool invert;
    [Tooltip("Has Card: the card that must be in the pockets, and how many. In Deck: the card that must be in the deck, and how many; none = that many cards of any kind.")]
    public CardData card;
    [Min(1)] public int count = 1;

    // A condition with no flag or quest picked yet is ignored.
    public bool IsMet()
    {
        if (type == StoryConditionType.QuestStatus)
            return quest == null || (QuestManager.GetStatus(quest) == questStatus) != invert;

        if (type == StoryConditionType.QuestObjectiveDone)
            return quest == null || QuestManager.IsObjectiveDone(quest) != invert;

        if (type == StoryConditionType.HasCard)
            return card == null || (Crafting.Owned(ExplorationPlayerSession.RuntimeInventory, card) >= Mathf.Max(1, count)) != invert;

        if (type == StoryConditionType.InDeck)
            return (CountInDeck(card) >= Mathf.Max(1, count)) != invert;

        if (type == StoryConditionType.CompanionWithPlayer)
            return CompanionWithPlayer() != invert;

        return string.IsNullOrWhiteSpace(storyFlag) || StoryFlagManager.HasFlag(storyFlag) != invert;
    }

    private static bool CompanionWithPlayer()
    {
        PlayerMovement player = UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();
        return player != null && CompanionFollower.IsPresentAt(player.transform.position);
    }

    private static int CountInDeck(CardData wanted)
    {
        DeckData deck = ExplorationPlayerSession.RuntimeDeck;
        if (deck == null)
            return 0;

        int found = 0;
        foreach (CardData inDeck in deck.Cards)
        {
            if (inDeck != null && inDeck.IsUsedInBattle && (wanted == null || inDeck == wanted))
                found++;
        }

        return found;
    }

    public static bool AreMet(IReadOnlyList<StoryCondition> conditions)
    {
        if (conditions == null)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            if (conditions[i] != null && !conditions[i].IsMet())
                return false;
        }

        return true;
    }
}
