using System.Collections.Generic;
using UnityEngine;

public abstract class BattleTargetingProfile : ScriptableObject
{
    [Header("Targeting Info")]
    [SerializeField] private string targetingID;
    [SerializeField] private string displayName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    public string TargetingID => targetingID;
    public string DisplayName => displayName;
    public string Description => description;

    public abstract IReadOnlyList<ICombatant> ResolveTargets(BattleTargetingContext ctx);
}
