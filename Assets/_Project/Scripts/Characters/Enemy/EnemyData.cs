using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Profile")]
    public CharacterProfile profile;

    [Header("Health")]
    public int maxHealth = 30;
    public int startingArmor = 0;

    [Header("Abilities")]
    public List<EnemyAbilityData> abilities = new();

    [Header("Loot")]
    public List<EnemyLootDrop> lootDrops = new();

    public string CharacterID => profile != null ? profile.CharacterID : name;
    public string DisplayName => profile != null ? profile.DisplayName : name;
    public string Description => profile != null ? profile.Description : string.Empty;
    public Sprite PortraitSprite => profile != null ? profile.PortraitSprite : null;
    public Sprite BattleSprite => PortraitSprite;
    public IReadOnlyList<EnemyLootDrop> LootDrops => lootDrops;

    private void OnValidate()
    {
        if (lootDrops == null)
            return;

        for (int i = 0; i < lootDrops.Count; i++)
            lootDrops[i]?.Validate();
    }
}
