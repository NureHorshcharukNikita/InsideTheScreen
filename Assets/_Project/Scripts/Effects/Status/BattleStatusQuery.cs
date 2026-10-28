using UnityEngine;

// What a fighter has on them right now.
public static class BattleStatusQuery
{
    public static bool Has(ICombatant combatant, BattleStatus status)
    {
        if (status == null || combatant is not Character character)
            return false;

        foreach (ActiveBattleStatus active in character.Statuses)
        {
            if (active != null && active.Status == status && !active.IsExpired)
                return true;
        }

        return false;
    }
}
