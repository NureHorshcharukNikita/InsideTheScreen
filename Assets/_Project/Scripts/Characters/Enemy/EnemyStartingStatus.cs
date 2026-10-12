using System;
using UnityEngine;

// A status an enemy enters the battle with (see EnemyData.startingStatuses).
[Serializable]
public sealed class EnemyStartingStatus
{
    public BattleStatus status;

    [Min(0)]
    public int value;

    [Tooltip("Ignored by permanent statuses.")]
    [Min(0)]
    public int duration;

    [Min(1)]
    public int stacks = 1;
}
