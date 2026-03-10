using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private PlayerCharacter player;
    [SerializeField] private EnemyCharacter enemy;

    [Header("Test Cards")]
    [SerializeField] private CardData testAttackCard;
    [SerializeField] private CardData testHealCard;

    public void PlayAttackCard()
    {
        if (testAttackCard == null)
            return;

        if (!player.SpendActionPoints(testAttackCard.Cost))
            return;

        CardResolver.Resolve(testAttackCard, player, enemy);
        CheckBattleState();
    }

    public void AttackPlayer()
    {
        if (testAttackCard == null)
            return;

        CardResolver.Resolve(testAttackCard, enemy, player);
        CheckBattleState();
    }

    public void PlayHealCard()
    {
        if (testHealCard == null)
            return;

        if (!player.SpendActionPoints(testHealCard.Cost))
            return;

        CardResolver.Resolve(testHealCard, player, enemy);
    }

    public void EndTurn()
    {
        player.RestoreActionPoints();
    }

    private void CheckBattleState()
    {
        if (enemy.CurrentHealth <= 0)
        {
            Debug.Log("Enemy defeated!");
        }

        if (player.CurrentHealth <= 0)
        {
            Debug.Log("Player defeated!");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            PlayAttackCard();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            AttackPlayer();

        if (Input.GetKeyDown(KeyCode.Alpha3))
            PlayHealCard();

        if (Input.GetKeyDown(KeyCode.Space))
            EndTurn();
    }
}