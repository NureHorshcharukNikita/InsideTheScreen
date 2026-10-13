using UnityEngine;

[CreateAssetMenu(menuName = "Game/Characters/Character Profile")]
public sealed class CharacterProfile : ScriptableObject
{
    [Header("Identity")]
    public string characterID;
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    [Header("Portrait")]
    public Sprite portraitSprite;
    [Tooltip("Which way the portrait art looks. In battle it is mirrored as needed so everyone faces their opponent.")]
    public SpriteFacing portraitFacing = SpriteFacing.Left;
    [Tooltip("Small picture shown in battle in place of the name, e.g. where an enemy's intent points. Empty = the name.")]
    public Sprite battleIcon;

    public string CharacterID => !string.IsNullOrWhiteSpace(characterID) ? characterID : name;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : name;
    public string Description => description;
    public Sprite PortraitSprite => portraitSprite;
    public SpriteFacing PortraitFacing => portraitFacing;
    public Sprite BattleIcon => battleIcon;
}
