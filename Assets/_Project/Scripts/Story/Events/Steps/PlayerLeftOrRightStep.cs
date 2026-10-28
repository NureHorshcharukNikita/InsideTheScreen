using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Runs one of two lists of steps, depending on whether the player is left or right of a point when
// it gets there, e.g. which end of the broken bridge Haru came to (Player Above Or Below the other way).
[Serializable, SubclassMenu("Flow/Player Left Or Right")]
public sealed class PlayerLeftOrRightStep : StoryStep
{
    [SerializeField] private Transform point;
    [SerializeReference, SubclassPicker] private List<StoryStep> whenLeft = new();
    [SerializeReference, SubclassPicker] private List<StoryStep> whenRight = new();

    public override bool IsInstant =>
        whenLeft.TrueForAll(step => step == null || step.IsInstant)
        && whenRight.TrueForAll(step => step == null || step.IsInstant);

    public override IEnumerator Run(StoryEventContext context)
    {
        foreach (StoryStep step in Branch(context))
            yield return context.Run(step);
    }

    public override void Skip(StoryEventContext context)
    {
        foreach (StoryStep step in Branch(context))
            step?.Skip(context);
    }

    public override IEnumerable<DialogueAction> GetActions()
    {
        foreach (StoryStep step in whenLeft.Concat(whenRight))
        {
            if (step == null)
                continue;

            foreach (DialogueAction action in step.GetActions())
                yield return action;
        }
    }

    private List<StoryStep> Branch(StoryEventContext context)
    {
        Transform player = context.Player;
        bool right = player != null && point != null && player.position.x > point.position.x;
        return right ? whenRight : whenLeft;
    }

    public override string ToString()
    {
        return $"Player left / right of {(point != null ? point.name : "(point)")}: {whenLeft.Count} / {whenRight.Count} steps";
    }
}
