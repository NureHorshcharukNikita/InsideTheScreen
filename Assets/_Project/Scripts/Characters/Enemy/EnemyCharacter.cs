using System.Collections.Generic;
using UnityEngine;

// A fighter the game controls (EnemyBrain): an enemy, or an ally at the player's side such as
// Hideshi, who is the same kind of fighter after JoinPlayerSide.
public class EnemyCharacter : Character
{
    [SerializeField] private EnemyData enemyData;
    private EnemyBrain brain;
    private Vector3 baseScale;
    private CombatTeam team = CombatTeam.Enemy;

    protected override void Awake()
    {
        base.Awake();
        brain = GetComponent<EnemyBrain>();
        baseScale = transform.localScale;
    }

    public override int MaxHealth => enemyData != null ? enemyData.maxHealth : 0;
    public override int StartingArmor => enemyData != null ? enemyData.startingArmor : 0;
    public override CombatTeam Team => team;
    public override string DisplayName => enemyData != null ? enemyData.DisplayName : name;
    public override Sprite BattleIcon => enemyData != null ? enemyData.BattleIcon : null;

    public EnemyData Data => enemyData;
    public EnemyBrain Brain => brain;
    public int ActionPointsPerTurn => enemyData != null ? Mathf.Max(1, enemyData.actionPointsPerTurn) : 1;

    public void JoinPlayerSide()
    {
        team = CombatTeam.Player;
    }

    public void ApplyStartingStatuses()
    {
        if (enemyData?.startingStatuses == null)
            return;

        BattleActionContext actionContext = BattleActionContext.CreateDefault();
        foreach (EnemyStartingStatus entry in enemyData.startingStatuses)
        {
            if (entry?.status == null)
                continue;

            ApplyStatus(entry.status, entry.duration, entry.stacks, entry.value, this, entry.status.ReapplyMode, actionContext);
        }
    }
    public IReadOnlyList<EnemyAbilityData> Abilities => enemyData != null
        ? enemyData.abilities
        : System.Array.Empty<EnemyAbilityData>();

    public void ApplyData(EnemyData data)
    {
        if (data == null)
            return;

        enemyData = data;
        transform.localScale = baseScale * Mathf.Max(0.01f, data.battleScale);
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
