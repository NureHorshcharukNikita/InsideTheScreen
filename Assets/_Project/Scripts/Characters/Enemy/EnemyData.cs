using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Profile")]
    public CharacterProfile profile;
    [Tooltip("Size in battle compared to other enemies: 0.7 = 30% smaller. For art drawn bigger or smaller than the rest.")]
    [Range(0.3f, 2f)]
    public float battleScale = 1f;

    [Header("Health")]
    public int maxHealth = 30;
    public int startingArmor = 0;

    [Header("Abilities")]
    [Tooltip("Spent on abilities each turn: with 2, the enemy can use two 1-point abilities or one 2-point ability.")]
    [Min(1)]
    public int actionPointsPerTurn = 1;
    public List<EnemyAbilityData> abilities = new();

    [Header("Passives")]
    [Tooltip("Applied to the enemy when the battle starts, e.g. a death ward. Make them permanent statuses to last the whole battle.")]
    public List<EnemyStartingStatus> startingStatuses = new();

    [Header("Loot")]
    public List<EnemyLootDrop> lootDrops = new();

    [Header("Story")]
    [Tooltip("Set when the player wins, e.g. so the NPC this fight came from disappears.")]
    public string storyFlagOnDefeat;

    public string CharacterID => profile != null ? profile.CharacterID : name;
    public string DisplayName => profile != null ? profile.DisplayName : name;
    public string Description => profile != null ? profile.Description : string.Empty;
    public Sprite PortraitSprite => profile != null ? profile.PortraitSprite : null;
    public Sprite BattleSprite => PortraitSprite;
    public Sprite BattleIcon => profile != null ? profile.BattleIcon : null;
    public IReadOnlyList<EnemyLootDrop> LootDrops => lootDrops;

    private void OnValidate()
    {
        if (lootDrops == null)
            return;

        for (int i = 0; i < lootDrops.Count; i++)
            lootDrops[i]?.Validate();
    }
}
