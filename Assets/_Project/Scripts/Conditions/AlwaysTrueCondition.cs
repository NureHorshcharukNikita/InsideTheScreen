using UnityEngine;

[CreateAssetMenu(menuName = "Game/Battle/Conditions/Always True", fileName = "Cond_AlwaysTrue")]
public class AlwaysTrueCondition : BattleCondition
{
    public override bool IsMet(BattleTargetingContext ctx) => true;
}
