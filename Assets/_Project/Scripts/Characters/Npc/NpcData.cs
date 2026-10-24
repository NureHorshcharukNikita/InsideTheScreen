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
    public List<NpcDialogueEntry> dialogueEntries = new();

    [Tooltip("Said instead of a dialogue that needs Hideshi while he is not with Haru, e.g. \"Where's your father?\". Empty = such dialogues wait and the next one is said.")]
    public DialogueData companionAwayDialogue;

    [Header("Overhead Speech")]
    public bool enableOverheadSpeech = true;

    [TextArea(1, 2)]
    public List<string> overheadLines = new();

    [Header("Trade")]
    public ShopData shop;

    [Tooltip("Trading straight away, when the NPC has nothing to say, only once all of these are met, e.g. Garr not before he has met Haru. A dialogue can still open the shop. Empty = always.")]
    public List<StoryCondition> tradeConditions = new();

    [Header("Movement")]
    public bool canMove;
    public float moveSpeed = 2f;

    [Header("Story")]
    [Tooltip("The NPC disappears once this story flag is set. Empty = always present.")]
    [StoryFlag] public string hiddenAfterStoryFlag;

    public bool CanTalk => enableDialogue && HasAnyDialogueEntry();
    public bool CanShowOverheadLines => enableOverheadSpeech && HasAnyText(overheadLines);
    public bool CanTrade => shop != null && shop.HasItems && StoryCondition.AreMet(tradeConditions);
    public ShopData Shop => shop;
    public string CharacterID => profile != null ? profile.CharacterID : name;
    public string DisplayName => profile != null ? profile.DisplayName : name;
    public string Description => profile != null ? profile.Description : string.Empty;
    public Sprite PortraitSprite => profile != null ? profile.PortraitSprite : null;

    public bool CanTalkWithState(string npcKey)
    {
        return SelectDialogue(npcKey, out _) != null;
    }

    // The first entry that can be said. One that needs Hideshi while he is away gives the Companion
    // Away dialogue instead (selectedEntry stays null: it does not count as a talk).
    public DialogueData SelectDialogue(string npcKey, out NpcDialogueEntry selectedEntry, bool companionPresent = true)
    {
        selectedEntry = null;
        if (!enableDialogue)
            return null;

        if (dialogueEntries != null)
        {
            for (int i = 0; i < dialogueEntries.Count; i++)
            {
                NpcDialogueEntry entry = dialogueEntries[i];
                if (entry == null || !entry.IsAvailable(npcKey))
                    continue;

                if (entry.needsCompanion && !companionPresent)
                {
                    if (companionAwayDialogue != null && companionAwayDialogue.HasLines)
                        return companionAwayDialogue;

                    continue;
                }

                selectedEntry = entry;
                return entry.dialogue;
            }
        }

        return null;
    }

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

    private bool HasAnyDialogueEntry()
    {
        if (dialogueEntries == null)
            return false;

        for (int i = 0; i < dialogueEntries.Count; i++)
        {
            if (dialogueEntries[i] != null && dialogueEntries[i].HasDialogue)
                return true;
        }

        return false;
    }

}
