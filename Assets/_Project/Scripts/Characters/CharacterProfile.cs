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

    public string CharacterID => !string.IsNullOrWhiteSpace(characterID) ? characterID : name;
    public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : name;
    public string Description => description;
    public Sprite PortraitSprite => portraitSprite;
}
