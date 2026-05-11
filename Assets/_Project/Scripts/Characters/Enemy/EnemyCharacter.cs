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
    public override CombatTeam Team => CombatTeam.Enemy;

    public EnemyData Data => enemyData;
    public EnemyBrain Brain => brain;

    public void ApplyEncounterTemplate(EnemyCharacter template)
    {
        if (template == null)
            return;

        enemyData = template.Data;
        CopySpriteFrom(template);

        SetFullHealth();
        RefreshColliderFromSprite();
    }

    private void CopySpriteFrom(EnemyCharacter template)
    {
        SpriteRenderer from = template.GetComponent<SpriteRenderer>();
        SpriteRenderer to = GetComponent<SpriteRenderer>();

        if (from?.sprite != null && to != null)
            to.sprite = from.sprite;
    }

    private void RefreshColliderFromSprite()
    {
        if (TryGetComponent(out BoxCollider2DSpriteSync colliderSync))
            colliderSync.RefreshFromSprite();
    }
}