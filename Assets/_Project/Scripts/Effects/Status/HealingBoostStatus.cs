using UnityEngine;

// Every heal the bearer gets is stronger while it lasts: Value more (a bandana), or Value percent
// more (a bracelet). Stacks add up.
[CreateAssetMenu(menuName = "Game/Battle/Statuses/Healing Boost", fileName = "HealingBoostStatus")]
public class HealingBoostStatus : BattleStatus
{
    [Tooltip("Value is a percent of the heal instead of a number of health points.")]
    [SerializeField] private bool percent;

    public static int Boost(ICombatant bearer, int amount)
    {
        if (amount <= 0 || bearer is not Character character)
            return amount;

        int boosted = amount;
        foreach (ActiveBattleStatus active in character.Statuses)
        {
            if (active == null || active.IsExpired || active.Status is not HealingBoostStatus boost)
                continue;

            int stacks = Mathf.Max(1, active.Stacks);
            boosted += boost.percent
                ? Mathf.RoundToInt(amount * active.Value * stacks / 100f)
                : active.Value * stacks;
        }

        return boosted;
    }
}
