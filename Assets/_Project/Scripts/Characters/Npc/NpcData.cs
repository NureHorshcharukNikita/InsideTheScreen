using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Characters/NPC Data")]
public sealed class NpcData : ScriptableObject
{
    [Header("Profile")]
    public CharacterProfile profile;

    [Header("Character")]
    public NpcDisposition disposition = NpcDisposition.Neutral;
    public int maxHealth = 1;
    public int startingArmor = 0;

    [Header("Dialogue")]
    public bool enableDialogue = true;
    public DialogueData dialogue;

    [Header("Overhead Speech")]
    public bool enableOverheadSpeech = true;

    [TextArea(1, 2)]
    public List<string> overheadLines = new();

    [Header("Quests")]
    public List<string> questKeys = new();

    [Header("Trade")]
    public List<ScriptableObject> tradeStock = new();

    [Header("Movement")]
    public bool canMove;
    public float moveSpeed = 2f;

    public bool CanTalk => enableDialogue && dialogue != null && dialogue.HasLines;
    public bool CanShowOverheadLines => enableOverheadSpeech && HasAnyText(overheadLines);
    public bool CanTrade => HasAnyAssignedAsset(tradeStock);
    public bool CanOfferQuests => HasAnyText(questKeys);
    public string CharacterID => profile != null ? profile.CharacterID : name;
    public string DisplayName => profile != null ? profile.DisplayName : name;
    public string Description => profile != null ? profile.Description : string.Empty;
    public Sprite PortraitSprite => profile != null ? profile.PortraitSprite : null;

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
