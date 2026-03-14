using UnityEngine;

public class CharacterClickable : MonoBehaviour
{
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private Character target;

    private void OnMouseDown()
    {
        battleSystem.OnTargetClicked(target);
    }
}