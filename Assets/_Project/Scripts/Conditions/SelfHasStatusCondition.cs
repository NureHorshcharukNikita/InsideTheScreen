using UnityEngine;

// True while the one using the ability has (or, with Must Have off, lacks) a status: lets an
// enemy switch abilities between phases.
[CreateAssetMenu(menuName = "Game/Battle/Conditions/Self Has Status", fileName = "Cond_SelfHasStatus")]
public class SelfHasStatusCondition : BattleCondition
{
    [SerializeField] private BattleStatus status;
    [SerializeField] private bool mustHave = true;

    public override bool IsMet(BattleTargetingContext context)
    {
        bool hasStatus = context.Self is Character self && self.HasStatus(status);
        return hasStatus == mustHave;
    }
}
