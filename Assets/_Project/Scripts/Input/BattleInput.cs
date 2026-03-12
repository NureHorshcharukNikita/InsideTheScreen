using UnityEngine;

public class BattleInput : MonoBehaviour
{
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private EnemyCharacter enemy;
    [SerializeField] private PlayerCharacter player;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            battleSystem.EndTurn();

        if (Input.GetKeyDown(KeyCode.E))
            battleSystem.OnTargetClicked(enemy);

        if (Input.GetKeyDown(KeyCode.P))
            battleSystem.OnTargetClicked(player);
    }
}