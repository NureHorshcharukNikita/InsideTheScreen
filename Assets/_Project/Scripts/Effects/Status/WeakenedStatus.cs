using UnityEngine;

// The bearer's blows land softer while it lasts: Value percent less damage (a glass bottle
// smashed on its head). Burning, poison and bleeding it puts on others are not affected.
[CreateAssetMenu(menuName = "Game/Battle/Statuses/Weakened", fileName = "WeakenedStatus")]
public class WeakenedStatus : BattleStatus
{
    public static int Weaken(ICombatant attacker, int amount)
    {
        if (amount <= 0 || attacker is not Character character)
            return amount;

        float share = 1f;
        foreach (ActiveBattleStatus active in character.Statuses)
        {
            if (active != null && !active.IsExpired && active.Status is WeakenedStatus)
                share *= 1f - Mathf.Clamp01(active.Value / 100f);
        }

        return Mathf.Max(0, Mathf.RoundToInt(amount * share));
    }
}
