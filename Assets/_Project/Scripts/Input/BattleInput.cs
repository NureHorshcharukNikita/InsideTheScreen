using UnityEngine;

public class BattleInput : MonoBehaviour
{
    [SerializeField] private BattleSystem battleSystem;

    private void Update()
    {
        if (!battleSystem.CanPlay()) return;

        if (Input.GetKeyDown(KeyCode.Space))
            battleSystem.EndTurn();
    }
}