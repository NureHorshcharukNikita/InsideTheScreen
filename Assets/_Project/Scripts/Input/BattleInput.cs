using UnityEngine;

public class BattleInput : MonoBehaviour
{
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private EnemyCharacter enemy;
    [SerializeField] private PlayerCharacter player;

    private void Update()
    {
        if (!battleSystem.CanPlay()) return;

        if (Input.GetKeyDown(KeyCode.Space))
            battleSystem.EndTurn();
    }
}