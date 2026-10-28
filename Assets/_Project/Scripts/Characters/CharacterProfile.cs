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

    public string CharacterID => !string.IsNullOrWhiteSpace(characterID) ? characterID : name;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : name;    public string Description => description;
    public Sprite PortraitSprite => portraitSprite;
    public SpriteFacing PortraitFacing => portraitFacing;
    public Sprite BattleIcon => battleIcon;
    public DialogueVoice Voice => voice;
}
