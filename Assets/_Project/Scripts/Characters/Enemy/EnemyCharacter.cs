using UnityEngine;

public class EnemyCharacter : Character
{
    [SerializeField] private EnemyData enemyData;
    private EnemyBrain brain;

    public override int MaxHealth => enemyData != null ? enemyData.maxHealth : 0;
    public override CombatTeam Team => CombatTeam.Enemy;

    public EnemyData Data => enemyData;
    public EnemyBrain Brain => brain != null ? brain : (brain = GetComponent<EnemyBrain>());

    public void ApplyEncounterTemplate(EnemyCharacter template)
    {
        if (template == null)
            return;

        enemyData = template.Data;

        SpriteRenderer from = template.GetComponent<SpriteRenderer>();
        SpriteRenderer to = GetComponent<SpriteRenderer>();
        if (from != null && to != null && from.sprite != null)
            to.sprite = from.sprite;

        SetMaxHeal();

        if (enemyData != null && Mathf.Abs(enemyData.battleLocalPositionYOffset) > 0f)
        {
            Vector3 lp = transform.localPosition;
            lp.y += enemyData.battleLocalPositionYOffset;
            transform.localPosition = lp;
        }

        if (TryGetComponent(out BoxCollider2DSpriteSync colliderSync))
            colliderSync.RefreshFromSprite();
    }
}