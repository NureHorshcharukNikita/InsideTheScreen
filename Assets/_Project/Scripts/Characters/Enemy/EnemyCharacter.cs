using System.Collections.Generic;
using UnityEngine;

public class EnemyCharacter : Character
{
    [SerializeField] private EnemyData enemyData;
    private EnemyBrain brain;

    protected override void Awake()
    {
        base.Awake();
        brain = GetComponent<EnemyBrain>();
    }

    public override int MaxHealth => enemyData != null ? enemyData.maxHealth : 0;
    public override int StartingArmor => enemyData != null ? enemyData.startingArmor : 0;
    public override CombatTeam Team => CombatTeam.Enemy;

    public EnemyData Data => enemyData;
    public EnemyBrain Brain => brain;
    public IReadOnlyList<EnemyAbilityData> Abilities => enemyData != null
        ? enemyData.abilities
        : System.Array.Empty<EnemyAbilityData>();

    public void ApplyData(EnemyData data)
    {
        if (data == null)
            return;

        enemyData = data;
        ApplyBattleSprite(data.BattleSprite);

        SetFullHealth();
        SetArmor(StartingArmor);
    }

    private void ApplyBattleSprite(Sprite sprite)
    {
        if (sprite == null)
            return;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            if (TryGetComponent(out BoxCollider2DSpriteSync colliderSync))
                colliderSync.RefreshFromSprite();
        }
    }
}
