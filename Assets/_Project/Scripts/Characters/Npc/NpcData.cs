using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Characters/NPC Data")]
public sealed class NpcData : ScriptableObject
{
    private const string DefaultTalkTopicsCloseChoice = "Nothing.";

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

    [Tooltip("When a story event walks Haru up to this NPC for a talk Hideshi speaks in (Walk Up and Talk step, Needs Companion), the father walks up in front and Haru stops a step behind him to the side, e.g. Eldon's first meeting. Talks started with the Interact key begin where everyone stands.")]
    public bool companionLeadsTalks;

    [Header("Talk Topics")]
    [Tooltip("Things the player can talk about with this NPC, offered as answers where a talk with it ends, e.g. asking Hideshi about this world while he waits, or telling him about the broken bridge Haru saw alone. Each one until it has been talked about; after one, the others are offered again. Empty = talks end as written.")]
    public List<NpcTalkTopic> talkTopics = new();

    [Tooltip("The answer next to them that ends the talk, e.g. \"Nothing. I'll get going.\"")]
    public string talkTopicsCloseChoice = DefaultTalkTopicsCloseChoice;

    [Header("Overhead Speech")]
    public bool enableOverheadSpeech = true;

    [TextArea(1, 2)]
    public List<string> overheadLines = new();

    [Header("Trade")]
    public ShopData shop;

    [Tooltip("Trading only once all of these are met, e.g. Garr not before Haru has done his first errand: until then neither the Trade key nor a dialogue answer opens the shop. Empty = always.")]
    public List<StoryCondition> tradeConditions = new();

    [Header("Movement")]
    public bool canMove;
    public float moveSpeed = 2f;

    [Header("Story")]
    [Tooltip("The NPC disappears once this story flag is set. Empty = always present.")]
    [StoryFlag] public string hiddenAfterStoryFlag;

    [Tooltip("Not around while all of these are met, and back once they no longer are, e.g. Garr at home for the night after Eldon's first talk, until the morning. Empty = always around.")]
    public List<StoryCondition> awayWhile = new();

    public bool IsAway => awayWhile != null && awayWhile.Count > 0 && StoryCondition.AreMet(awayWhile);

    public bool CanTalk => enableDialogue && HasAnyDialogueEntry();
    public bool CanShowOverheadLines => enableOverheadSpeech && HasAnyText(overheadLines);
    public bool CanTrade => shop != null && shop.HasItems && StoryCondition.AreMet(tradeConditions);
    public ShopData Shop => shop;
    public string TalkTopicsCloseText => string.IsNullOrWhiteSpace(talkTopicsCloseChoice) ? DefaultTalkTopicsCloseChoice : talkTopicsCloseChoice;
    public string CharacterID => profile != null ? profile.CharacterID : name;
    public string DisplayName => profile != null ? profile.DisplayName : name;
    public string Description => profile != null ? profile.Description : string.Empty;
    public Sprite PortraitSprite => profile != null ? profile.PortraitSprite : null;
    public Sprite MapSprite => profile != null ? profile.MapSprite : null;

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
