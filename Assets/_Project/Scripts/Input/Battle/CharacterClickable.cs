using UnityEngine;

public class CharacterClickable : MonoBehaviour
{
    [SerializeField] private BattleSystem battleSystem;
    [SerializeField] private Character target;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<Character>();
        if (battleSystem == null)
            battleSystem = FindAnyObjectByType<BattleSystem>();
    }

    private void OnMouseDown()
    {
        if (battleSystem == null || target == null)
            return;

        battleSystem.OnTargetClicked(target);
    }
}
