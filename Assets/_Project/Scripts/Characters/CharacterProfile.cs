using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Characters/Character Profile")]
public sealed class CharacterProfile : ScriptableObject
{
    // One step in getting to know someone's name (KnownNames), e.g. "Father" once Haru says Dad.
    [Serializable]
    public sealed class HeardName
    {
        [Tooltip("Words or a phrase that make it known when a dialogue line says one, e.g. Dad, Eldon, or \"I'm Hideshi\" for a name known only once he introduces himself.")]
        public List<string> heardAs = new();
        [Tooltip("What the name plate shows from then on, e.g. Father. Empty = the name itself.")]
        public string shownAs;
    }

    [Header("Identity")]
    public string characterID;
    public string displayName;
    [Tooltip("How the name gets known, step by step: \"...\" until the first is heard, then the last one heard in this list, e.g. Father after Dad, then Hideshi after \"I'm Hideshi\". Empty = known from the start, e.g. the Trader.")]
    public List<HeardName> namesHeard = new();

    [TextArea(2, 4)]
    public string description;

    [Header("Portrait")]
    public Sprite portraitSprite;
    [Tooltip("Which way the portrait art looks. In battle it is mirrored as needed so everyone faces their opponent.")]
    public SpriteFacing portraitFacing = SpriteFacing.Left;
    [Tooltip("Small picture shown in battle in place of the name, e.g. where an enemy's intent points. Empty = the name.")]
    public Sprite battleIcon;

    [Header("Dialogue")]
    [Tooltip("How their lines sound while they type out. Empty = silent.")]
    public DialogueVoice voice;

    [Header("Map")]
    [Tooltip("How they look walking on the map, e.g. the small Mini sprite of an NPC. Empty = an NPC keeps the sprite set on it; an enemy shows its portrait.")]
    public Sprite mapSprite;

    [Header("Look Before")]
    [Tooltip("Until this story flag is set they look different, e.g. Haru without his bags until Garr gives them: Portrait Before in dialogues, and on the map or in battle whatever a Story Look on them shows. Empty = always as above.")]
    [StoryFlag] public string lookChangesAt;
    [Tooltip("Their portrait until Look Changes At is set. Empty = Portrait Sprite.")]
    public Sprite portraitBefore;

    // How they look from a point of the story on (Looks Later).
    [Serializable]
    public sealed class LaterLook
    {
        [Tooltip("From when: once this story flag is set, e.g. a1_leymar_morning for the trader's bitten hand.")]
        [StoryFlag] public string from;
        [Tooltip("Their portrait from then on. Empty = as before.")]
        public Sprite portrait;
        [Tooltip("How they look on the map from then on. Empty = as before.")]
        public Sprite mapSprite;
    }

    [Header("Looks Later")]
    [Tooltip("Their look changing as the story goes on: the last one whose flag is set is how they look, e.g. the trader's bitten hand from dawn, bandaged once he has the salve. Empty = always as above.")]
    public List<LaterLook> laterLooks = new();

    // The profiles loaded, to tell whose usual portrait a dialogue line names by hand (AsNow).
    private static readonly List<CharacterProfile> Loaded = new();

    public string CharacterID => !string.IsNullOrWhiteSpace(characterID) ? characterID : name;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : name;    public string Description => description;
    public bool LooksAsBefore => !string.IsNullOrWhiteSpace(lookChangesAt) && !StoryFlagManager.HasFlag(lookChangesAt);
    public Sprite PortraitSprite => portraitBefore != null && LooksAsBefore ? portraitBefore
        : LaterSprite(look => look.portrait) ?? portraitSprite;
    public SpriteFacing PortraitFacing => portraitFacing;
    public Sprite BattleIcon => battleIcon;
    public Sprite MapSprite => LaterSprite(look => look.mapSprite) ?? mapSprite;

    // The latest look the story has reached that has this sprite; null for none.
    private Sprite LaterSprite(Func<LaterLook, Sprite> sprite)
    {
        for (int i = (laterLooks?.Count ?? 0) - 1; i >= 0; i--)
        {
            LaterLook look = laterLooks[i];
            if (look == null || sprite(look) == null || string.IsNullOrWhiteSpace(look.from))
                continue;

            if (StoryFlagManager.HasFlag(look.from))
                return sprite(look);
        }

        return null;
    }
    public DialogueVoice Voice => voice;

    // A portrait set by hand on a dialogue line that is someone's usual one shows as theirs is now,
    // e.g. Haru's without his bags before Garr gives them.
    public static Sprite AsNow(Sprite portrait)
    {
        if (portrait == null)
            return null;

        foreach (CharacterProfile profile in Loaded)
        {
            if (profile != null && profile.portraitSprite == portrait)
                return profile.PortraitSprite;
        }

        return portrait;
    }

    private void OnEnable()
    {
        if (!Loaded.Contains(this))
            Loaded.Add(this);
    }

    private void OnDisable()
    {
        Loaded.Remove(this);
    }
}
