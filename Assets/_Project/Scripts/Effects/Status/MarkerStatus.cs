using UnityEngine;

// A status with no effect of its own: it marks a state (e.g. an enemy's second phase) that
// abilities check through Self Has Status conditions, and shows the player what changed.
[CreateAssetMenu(menuName = "Game/Battle/Statuses/Marker", fileName = "MarkerStatus")]
public class MarkerStatus : BattleStatus
{
}
