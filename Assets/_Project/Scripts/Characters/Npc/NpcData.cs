using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Characters/NPC Data")]
public sealed class NpcData : ScriptableObject
{
    [Header("Identity")]
    public string npcID;
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Header("Character")]
    public NpcDisposition disposition = NpcDisposition.Neutral;
    public int maxHealth = 1;
    public int startingArmor = 0;
    public Sprite worldSprite;
    public Sprite dialogueSprite;

    [Header("Dialogue")]
    [TextArea(2, 4)]
    public List<string> dialogueLines = new();

    [Header("Overhead Lines")]
    [TextArea(1, 2)]
    public List<string> overheadLines = new();

    [Header("Quests")]
    public List<string> questIDs = new();

    [Header("Trade")]
    public List<ScriptableObject> tradeStock = new();

    [Header("Movement")]
    public bool canMove;
    public float moveSpeed = 2f;

    public bool CanTalk => HasAnyText(dialogueLines);
    public bool CanShowOverheadLines => HasAnyText(overheadLines);
    public bool CanTrade => HasAnyAssignedAsset(tradeStock);
    public bool CanOfferQuests => HasAnyText(questIDs);

    private static bool HasAnyText(List<string> values)
    {
        if (values == null)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return true;
        }

        return false;
    }

    private static bool HasAnyAssignedAsset(List<ScriptableObject> values)
    {
        if (values == null)
            return false;

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] != null)
                return true;
        }

        return false;
    }
}
