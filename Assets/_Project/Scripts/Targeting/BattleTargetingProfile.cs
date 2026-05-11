using System.Collections.Generic;
using UnityEngine;

public abstract class BattleTargetingProfile : ScriptableObject
{
    public abstract IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx);
}
